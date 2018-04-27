#region

using System;

#endregion

namespace ExpressionFilter.Contracts
{
    public interface IToken
    {
        T Value<T>() where T : IConvertible;
    }
}