namespace CleanBusMediator.Core
{
    public interface IEventHandler<in TEvent>
        where TEvent : IEvent
    {
        Task Handle(TEvent notification, CancellationToken cancellationToken);
    }
}