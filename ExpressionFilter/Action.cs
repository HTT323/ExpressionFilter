#region

using JetBrains.Annotations;

#endregion

namespace ExpressionFilter
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum Action
    {
        Compare,
        All,
        Any,
        Count,
        Contains
    }
}