using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Extensions;

public static class TaskExtensions
{
    public static async IAsyncEnumerable<Task<T>> WhenAny<T>(this IEnumerable<Task<T>> tasks, int batchSize)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be positive.");
        }

        var processingTasks = new HashSet<Task<T>>();

        foreach (var task in tasks)
        {
            processingTasks.Add(task);

            if (processingTasks.Count == batchSize)
            {
                var completedTask = await Task.WhenAny(processingTasks);
                processingTasks.Remove(completedTask);
                yield return completedTask;
            }
        }

        while (processingTasks.Any())
        {
            var completedTask = await Task.WhenAny(processingTasks);
            processingTasks.Remove(completedTask);

            yield return completedTask;
        }
    }
}
