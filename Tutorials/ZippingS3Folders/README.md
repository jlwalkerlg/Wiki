# Zipping S3 folders with the `Dataflow` namespace

The `System.Threading.Tasks.Dataflow` namespace from .NET makes it relatively simple to perform asynchronous actions in parallel, and to define how many asynchronous actions can be running at any given time.

This sample shows how to use the `TransformBlock` and `ActionBlock` classes from the `System.Threading.Tasks.Dataflow` namespace to download S3 files concurrently and add them synchronously to a zip archive.

To do so, we first instantiate a `TransformBlock` with a user-defined, asynchronous transform function.

```csharp
var downloader = new TransformBlock<S3Object, GetObjectResponse>(async s3Object =>
{
    return await s3Client.GetObjectAsync(s3Object.BucketName, s3Object.Key);
}, new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = 20,
});
```

The `ExecutionDataflowBlockOptions.MaxDegreeOfParallelism` property defines how many executions of our transform function can be executing at any given time, so in this case we specify that we want to allow no more than 20 objects to be concurrently downloading from S3.

We then pipe the results of this transform into an `ActionBlock`, which can operates on the results with a different degree of parallelism. Since the entries can only be added to zip archives synchronously, we specify a max degree of parallelism of 1.

```csharp
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
```

To pipe the results from the `TransformBlock` into the `ActionBlock`, you link them together like so.

```csharp
downloader.LinkTo(zipper);
```

If the objects are downloaded faster than they can be zipped, they'll be buffered in memory until the `ActionBlock` is ready to process the next one. Hence, it's worth tuning the degree of parallelism for the downloads to make sure that the zipping process doesn't become a bottleneck.

To actually feed the `TransformBlock` with items to process, we call the `TransformBlock.SendAsync()` method for each item.

```csharp
ListObjectsV2Response? response = null;
do
{
    response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
    {
        BucketName = bucket,
        Prefix = prefix,
        ContinuationToken = response?.NextContinuationToken,
    }, cancellationToken).ConfigureAwait(false);

    foreach (var s3Object in response.S3Objects)
    {
        await download.SendAsync(s3Object, cancellationToken);
    }
}
while (response?.IsTruncated is true && !cancellationToken.IsCancellationRequested);
```

If the `TransformBlock` is already processing as many items as it can in parallel, the `TransformBlock.SendAsync()` method will wait until it can accept another item.

To wait until all items have been processed, we first let the `TransformBlock` know that there are no more items waiting to be processed, and then wait for it to finish processing any items that it is currently processing.

```csharp
downloader.Complete();
await downloader.Completion;
```

At this point, the `TransformBlock` has already processed all items, and as such we just need to wait for the `ActionBlock` to finish handling them.

```csharp
zipper.Complete();
await zipper.Completion;
```

We then dispose the zip archive, which is necessary to write important bytes to the zip file (otherwise the zip file is corrupted).

```csharp
archive.Dispose();
```
