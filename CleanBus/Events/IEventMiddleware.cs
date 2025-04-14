using CleanBusMediator.Core;

namespace CleanBusMediator.Events
{
    public delegate Task EventHandlerDelegate();

    public interface IEventMiddleware<in TEvent>
        where TEvent : IEvent
    {
        Task Handle(TEvent @event, CancellationToken cancellationToken, EventHandlerDelegate next);
    }
}
