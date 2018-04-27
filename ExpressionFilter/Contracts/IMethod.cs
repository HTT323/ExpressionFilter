namespace ExpressionFilter.Contracts
{
    public interface IMethod
    {
        bool Evaluate<T>(T entity);
    }
}