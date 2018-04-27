#region

using System.Collections.Generic;

#endregion

namespace ExpressionFilter.Contracts
{
    public interface ITokenModule
    {
        IDictionary<string, IToken> GetTokens();
    }
}