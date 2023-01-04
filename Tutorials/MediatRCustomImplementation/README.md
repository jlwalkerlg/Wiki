# Adding a custom MediatR `IPublisher` implementation

The default `IPublisher` implementation within MediatR is to publish the notification to each handler synchronously, and fail if any one of those handlers throw an error.

To override this behaviour, it is necessary to write your own custom `IPublisher` implementation and register _it_ within the dependency injection container instead of the default implementation.

To do so, create a class which extends the `Mediator` class from MediatR, and override the `PublishCore` method, which is where you actually send the notification to each handler.

```csharp
public class PublishAndForgetMediator : Mediator
{
    private readonly ILogger<PublishAndForgetMediator> _logger;

    public PublishAndForgetMediator(ServiceFactory serviceFactory, ILogger<PublishAndForgetMediator> logger) : base(serviceFactory)
    {
        _logger = logger;
    }

    protected override Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification, CancellationToken cancellationToken)
    {
        foreach (var handler in allHandlers)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await handler(notification, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error occured in a handler while handling notification {NotificationType}.", notification.GetType());
                }
            }, cancellationToken);
        }

        return Task.CompletedTask;
    }
}
```

In the above example, the notification is sent to all handlers in parallel, and any exceptions thrown are logged but do not prevent the notification from being sent to the rest of the handlers.

To override the default implementation with your custom implementation in the dependency injection container, the `MediatR.Extensions.Microsoft.DependencyInjection` package provides an overload of the `AddMediatR` extension method.

```csharp
services.AddMediatR(config => config.Using<PublishAndForgetMediator>(), typeof(Startup));
```

Alternatively, if you're not using this package (for example if you're using Autofac), then your can register your custom implementation manually, ensuring that it's registered as a transient dependency and after the default implementation is registered. Dependening on how you inject the service into your application code, it may need to be registered as both an implementation of `IPublisher` and `IMediator`. Autofac example using the `MediatR.Extensions.Autofac.DependencyInjection` package:

```csharp
builder.RegisterMediatR(typeof(Startup).Assembly);
builder.RegisterType<PublishAndForgetMediator>().As<IPublisher>().As<IMediator>().InstancePerDependency();
```
