namespace CleanBusMediator.Configuration
{
    public interface ICommandPipelineBuilder
    {
        IEnumerable<object> Build(Type commandType, Type resultType);
    }
}
