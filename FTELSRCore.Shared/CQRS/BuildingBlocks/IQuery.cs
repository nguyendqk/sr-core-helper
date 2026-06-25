namespace FTELSRCore.CQRS.BuildingBlocks
{
    public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse : notnull;
}