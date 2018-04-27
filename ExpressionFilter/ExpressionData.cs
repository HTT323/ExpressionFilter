#region

using JetBrains.Annotations;

#endregion

namespace ExpressionFilter
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ExpressionData
    {
        public ExpressionType Type { get; set; }
        public FilterExpression Expression { get; set; }
        public Filter Filter { get; set; }
    }
}