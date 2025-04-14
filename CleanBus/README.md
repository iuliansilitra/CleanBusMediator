# 🚍 CleanBusMediator

**CleanBusMediator** is a modern, modular, and extensible **Command Bus / Mediator framework** for .NET.  
It promotes a clean separation of responsibilities through Commands, Events, Streaming, Middleware Pipelines, Exception Handling, and more — all with full support for dependency injection and testability.

> Think of it as your lightweight, powerful message dispatcher — without the magic.

---

## 📦 Installation

Coming soon to NuGet:

```bash
dotnet add package CleanBusMediator
```

---

## 🔧 Quick Setup

### 1. Register CleanBusMediator in your DI container

CleanBusMediator supports multiple registration styles:

#### ➤ Standard (recommended for most apps)

```csharp
builder.Services.CleanBusMediator(typeof(CreateUserHandler).Assembly);
```

This scans the specified assembly for:

- Command handlers
- Event handlers
- Stream handlers
- Middlewares
- Exception handlers
- Pre-/Post-processors

---

#### ➤ Fluent configuration (advanced)

```csharp
builder.Services.CleanBusMediator(config =>
{
    config.RegisterFromAssembly(typeof(CreateUserHandler).Assembly);

    // Optional: add your own middleware behaviors
    config.AddMiddleware(typeof(ResultValidationMiddleware<,>));
    config.AddMiddleware(typeof(MyCustomLoggingBehavior<,>));
});
```

---

#### ➤ From Type (shorthand)

```csharp
builder.Services.CleanBusMediator(typeof(CreateUserHandler));
```

---

#### ➤ Fallback: auto-discover all assemblies (use with caution)

```csharp
builder.Services.CleanBusMediator();
```

> ⚠️ This will scan all loaded assemblies in `AppDomain.CurrentDomain`.  
Use only in small apps, tests, or single-project setups.

---

## ✉️ Send Commands

### Define a command

```csharp
public class PingCommand : ICommand<string> { }
```

### Handle the command

```csharp
public class PingHandler : ICommandHandler<PingCommand, string>
{
    public Task<string> Handle(PingCommand command, CancellationToken cancellationToken)
        => Task.FromResult("Pong!");
}
```

### Dispatch it

```csharp
var result = await dispatcher.Send(new PingCommand());
// result: "Pong!"
```

---

## 📢 Publish Events

### Define an event

```csharp
public class OrderPlaced : IEvent
{
    public Guid OrderId { get; set; }
}
```

### Handle the event

```csharp
public class SendConfirmationEmail : IEventHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Email sent for order {notification.OrderId}");
        return Task.CompletedTask;
    }
}
```

### Publish it

```csharp
await dispatcher.Publish(new OrderPlaced { OrderId = Guid.NewGuid() });
```

---

## 🌀 Streaming Commands

### Define a stream command

```csharp
public class StreamNumbers : IStreamCommand<int>
{
    public int Count { get; set; }
}
```

### Handle it

```csharp
public class StreamNumbersHandler : IStreamCommandHandler<StreamNumbers, int>
{
    public async IAsyncEnumerable<int> Handle(StreamNumbers command, CancellationToken cancellationToken)
    {
        for (int i = 0; i < command.Count; i++)
        {
            yield return i;
            await Task.Delay(100);
        }
    }
}
```

### Consume the stream

```csharp
await foreach (var number in dispatcher.Stream(new StreamNumbers { Count = 5 }))
{
    Console.WriteLine(number);
}
```

---

## 🧩 Middleware Support

### Define a middleware

```csharp
public class LoggingMiddleware<TCommand, TResult> : ICommandMiddleware<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(TCommand command, CancellationToken cancellationToken, CommandHandlerDelegate<TResult> next)
    {
        Console.WriteLine($"[Start] {typeof(TCommand).Name}");
        var result = await next();
        Console.WriteLine($"[End] {typeof(TCommand).Name}");
        return result;
    }
}
```

Registered automatically if in the scanned assembly.

---

## 🛡️ Exception Handling

### Define an exception handler

```csharp
public class PingExceptionHandler : ICommandExceptionHandler<PingCommand, string>
{
    public Task<string> Handle(PingCommand command, Exception exception, CancellationToken cancellationToken)
    {
        return Task.FromResult("Recovered from error");
    }
}
```

---

## 🧪 Pre- and Post-Processors

### Pre-processor

```csharp
public class ValidatePing : ICommandPreProcessor<PingCommand>
{
    public Task Process(PingCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Pre-processing PingCommand");
        return Task.CompletedTask;
    }
}
```

### Post-processor

```csharp
public class LogPingResult : ICommandPostProcessor<PingCommand, string>
{
    public Task Process(PingCommand command, string result, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Post-processing result: {result}");
        return Task.CompletedTask;
    }
}
```

---

## 🌍 Event Middleware

```csharp
public class EventLoggingMiddleware<TEvent> : IEventMiddleware<TEvent>
    where TEvent : IEvent
{
    public async Task Handle(TEvent @event, CancellationToken cancellationToken, EventHandlerDelegate next)
    {
        Console.WriteLine($"[Event Start] {@event.GetType().Name}");
        await next();
        Console.WriteLine($"[Event End] {@event.GetType().Name}");
    }
}
```

---

## ⚡ Parallel Event Publishing

Optional: run event handlers in parallel

```csharp
await dispatcher.PublishParallel(new OrderPlaced { OrderId = Guid.NewGuid() });
```

---

## 🧪 Testing Support

### Fake Dispatcher

```csharp
var dispatcher = new FakeDispatcher
{
    SendHandler = cmd => "FakeResult"
};

var result = await dispatcher.Send(new PingCommand());
```

### Test Dispatcher (manual handler registration)

```csharp
var testDispatcher = new TestDispatcher();
testDispatcher.RegisterHandler(new PingHandler());

var result = await testDispatcher.Send(new PingCommand());
```

---

## 🔁 Retry Policies

Custom retry logic per command:

```csharp
public class SimpleRetryPolicyProvider : IRetryPolicyProvider
{
    public AsyncPolicy<TResult>? GetPolicy<TCommand, TResult>()
        where TCommand : ICommand<TResult>
    {
        return Policy<TResult>
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200));
    }
}
```

Used automatically via `RetryMiddlewareFactory<,>` if registered.

---

## 🧠 Scoped Pipeline Builder

Need different middlewares per request? Use `ScopedCommandPipelineBuilder`:

```csharp
var scopedDispatcher = new Dispatcher(
    type => scopedProvider.GetRequiredService(type),
    new ScopedCommandPipelineBuilder(scopedProvider, defaultPipeline)
);
```

---

## 🧰 Built-in Middleware Available

- ✅ `LoggingMiddleware`
- ✅ `ValidationMiddleware` (via `IValidator<>`)
- ✅ `CachingMiddleware` (for `ICachableCommand`)
- ✅ `RetryMiddleware` (via `IRetryPolicyProvider`)

---

## 🧾 License

Licensed under the MIT License.
