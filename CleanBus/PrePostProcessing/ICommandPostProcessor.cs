namespace CleanBusMediator.PrePostProcessing
{
    public interface ICommandPreProcessor<in TCommand>
    {
        Task Process(TCommand command, CancellationToken cancellationToken);
    }
}
