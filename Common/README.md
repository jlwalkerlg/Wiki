# Common

## TaskExtensions

### WhenAny()

Takes an `IEnumerable` of tasks, and a batch size, and returns an `IAsyncEnumerable` that yields the result of each task, one-by-one, as and when they complete. Only executes up to a given number of the tasks at any given time, as defined by the batch size.

Example usage:

```csharp
var tasks = Enumerable.Range(1, 100).Select(async i =>
{
    await Task.Delay(random.Next(100, 200));
    return i;
});

await foreach (var i in tasks.WhenAny(10))
{
    Console.WriteLine(i);
}
```

- `WhenAny()` will start executing the first 10 tasks, then wait for one to complete before starting the next.
- As each task completes, it will be yielded to the `foreach` loop.
- There are always therefore 10 tasks running at any given time, except when there are less than 10 tasks remaining.
