#region

using System.Collections.Generic;

#endregion

namespace ExpressionFilter.Contracts
{
    public interface IMethodModule
    {
        IDictionary<string, IMethod> GetMethods();
    }
}