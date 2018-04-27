#region

using System.Collections.Generic;
using JetBrains.Annotations;

#endregion

namespace ExpressionFilter
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Filter
    {
        public LogicalOperator Operator { get; set; }
        public IEnumerable<ExpressionData> Expressions { get; set; }
    }
}