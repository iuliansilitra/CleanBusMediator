namespace CleanBusMediator.PrePostProcessing
{
    public interface ICommandPostProcessor<in TCommand, in TResult>
    {
        Task Process(TCommand command, TResult result, CancellationToken cancellationToken);
    }
}
