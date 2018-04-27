#region

using JetBrains.Annotations;

#endregion

namespace ExpressionFilter
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class FilterExpression
    {
        public PropertyType PropertyType { get; set; }
        public Action Action { get; set; }
        public string PropertyId { get; set; }
        public DataType Type { get; set; }
        public string Left { get; set; }
        public Operator Operator { get; set; }
        public object Right { get; set; }
        public object Data { get; set; }
        public DataType? DataType { get; set; }
        public string Token { get; set; }
    }
}