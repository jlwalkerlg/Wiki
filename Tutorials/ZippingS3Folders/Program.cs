using Amazon.S3;
using Amazon.S3.Model;
using System.IO.Compression;
using System.Threading.Tasks.Dataflow;

var awsAccessKeyId = "";
var awsSecretAccessKey = "";
var region = Amazon.RegionEndpoint.EUCentral1;

var bucket = "";
var prefix = "";

var outputZipFilePath = "";

using var zip = File.Open(outputZipFilePath, FileMode.Create);
using var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true);

using var s3Client = new AmazonS3Client(
    awsAccessKeyId: awsAccessKeyId,
    awsSecretAccessKey: awsSecretAccessKey,
    region: Amazon.RegionEndpoint.EUCentral1);

var downloader = new TransformBlock<S3Object, GetObjectResponse>(async s3Object =>
{
    return await s3Client.GetObjectAsync(s3Object.BucketName, s3Object.Key);
}, new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = 20,
});

var zipper = new ActionBlock<GetObjectResponse>(async getObjectResponse =>
{
    var zipEntry = archive.CreateEntry(Path.GetFileName(getObjectResponse.Key), CompressionLevel.Fastest);

    await using var zipEntryStream = zipEntry.Open();
    await getObjectResponse.ResponseStream.CopyToAsync(zipEntryStream);

    await getObjectResponse.ResponseStream.DisposeAsync();
    getObjectResponse.Dispose();
}, new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = 1,
});

downloader.LinkTo(zipper);

ListObjectsV2Response? response = null;
do
{
    response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
    {
        BucketName = bucket,
        Prefix = prefix,
        ContinuationToken = response?.NextContinuationToken,
    }).ConfigureAwait(false);

    foreach (var s3Object in response.S3Objects)
    {
        await downloader.SendAsync(s3Object);
    }
}
while (response?.IsTruncated is true);

downloader.Complete();
await downloader.Completion;

zipper.Complete();
await zipper.Completion;

archive.Dispose();
