using Amazon.S3;
using Amazon.S3.Model;
using JW.AmazonS3.Models;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading.Tasks.Dataflow;

namespace JW.AmazonS3;

public static class AmazonS3ClientExtensions
{
    public static async Task ZipDirectory(
        this AmazonS3Client amazonS3Client,
        string bucketName,
        string s3Directory,
        string outputZipFilePath,
        ZipDirectoryOptions? options = default,
        CancellationToken cancellationToken = default)
    {
        options ??= new ZipDirectoryOptions();

        using var zip = File.Open(outputZipFilePath, FileMode.Create);
        using var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: false);

        var downloader = new TransformBlock<S3Object, GetObjectResponse>(async s3Object =>
        {
            return await amazonS3Client.GetObjectAsync(s3Object.BucketName, s3Object.Key, cancellationToken);
        }, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = options.DownloadsMaxDegreeOfParallelism,
        });

        var zipper = new ActionBlock<GetObjectResponse>(async getObjectResponse =>
        {
            var zipEntry = archive.CreateEntry(Path.GetFileName(getObjectResponse.Key), CompressionLevel.Fastest);

            await using var zipEntryStream = zipEntry.Open();
            await getObjectResponse.ResponseStream.CopyToAsync(zipEntryStream, cancellationToken);

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
            response = await amazonS3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = s3Directory,
                ContinuationToken = response?.NextContinuationToken,
            }, cancellationToken).ConfigureAwait(false);

            foreach (var s3Object in response.S3Objects)
            {
                await downloader.SendAsync(s3Object, cancellationToken);
            }
        }
        while (response?.IsTruncated is true);

        downloader.Complete();
        await downloader.Completion;

        zipper.Complete();
        await zipper.Completion;
    }
}
