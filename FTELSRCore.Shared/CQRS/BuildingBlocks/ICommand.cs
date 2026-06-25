namespace FTELSRCore.CQRS.BuildingBlocks
{
    public interface ICommand<out TResponse> : IRequest<TResponse> where TResponse : notnull;
}