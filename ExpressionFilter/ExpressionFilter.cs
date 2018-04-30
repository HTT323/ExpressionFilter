#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionFilter.Contracts;
using Newtonsoft.Json;

#endregion

namespace ExpressionFilter
{
    public class ExpressionFilter
    {
        private IDictionary<string, IMethod> _methods;
        private int _peCounter = 1;
        private IDictionary<string, IToken> _tokens;

        public Expression<Func<T, bool>> Build<T>(Filter filter)
        {
            return Build<T>(filter, null, null);
        }

        public Expression<Func<T, bool>> Build<T>(Filter filter, ITokenModule tokenModule, IMethodModule methodModule)
        {
            _tokens =
                tokenModule == null
                    ? new Dictionary<string, IToken>()
                    : tokenModule.GetTokens();

            _methods =
                methodModule == null
                    ? new Dictionary<string, IMethod>()
                    : methodModule.GetMethods();

            var pe = Expression.Parameter(typeof(T), "f");
            var be = BoolExpression(filter, null, pe);
            var lambda = Expression.Lambda<Func<T, bool>>(be, pe);

            return lambda;
        }

        private Expression BoolExpression(
            Filter filter,
            Expression rootExpression,
            Expression parameterExpression)
        {
            var root = rootExpression;
            var counter = 1;

            Expression left = null;
            Expression right = null;

            foreach (var exp in filter.Expressions)
            {
                var ex = CreateExpression(parameterExpression, exp);

                if (counter % 2 == 1)
                    left = ex;

                if (counter % 2 == 0)
                    right = ex;

                if (counter % 2 == 0)
                {
                    Debug.Assert(left != null, "left != null");
                    Debug.Assert(right != null, "right != null");

                    switch (filter.Operator)
                    {
                        case LogicalOperator.And:

                            root = root == null
                                ? Expression.AndAlso(left, right)
                                : Expression.AndAlso(root, Expression.AndAlso(left, right));

                            break;

                        case LogicalOperator.Or:

                            root = root == null
                                ? Expression.OrElse(left, right)
                                : Expression.OrElse(root, Expression.OrElse(left, right));

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                counter++;
            }

            if (filter.Expressions.Count() % 2 != 1) return root;

            Debug.Assert(left != null, "left != null");

            switch (filter.Operator)
            {
                case LogicalOperator.And:

                    root = root == null
                        ? left
                        : Expression.AndAlso(root, left);

                    break;

                case LogicalOperator.Or:

                    root = root == null
                        ? left
                        : Expression.OrElse(root, left);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return root;
        }

        private Expression CreateExpression(Expression parameterExpression, ExpressionData exp)
        {
            Expression ex;

            switch (exp.Type)
            {
                case ExpressionType.Condition:
                    ex = BoolExpression(exp.Filter, null, parameterExpression);
                    break;

                case ExpressionType.Expression:

                    Expression ep;

                    switch (exp.Expression.PropertyType)
                    {
                        case PropertyType.Value:
                        case PropertyType.Collection:
                        case PropertyType.Key:
                        case PropertyType.BasicCollection:
                        case PropertyType.StringContains:
                            ep = BuilderUtility.GetProperty(parameterExpression, exp.Expression.PropertyId);
                            break;

                        case PropertyType.Method:
                            ep = BuilderUtility.CallMethod(parameterExpression, exp, _methods);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    ex = BuildExpression(ep, exp);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ex;
        }

        private Expression BuildExpression(Expression ep, ExpressionData exp)
        {
            Expression ex;

            var dataType = exp.Expression.Type;
            var ce = BuilderUtility.BuildConstantExpression(_tokens, ep, exp, dataType);
            var memberType = BuilderUtility.GetDataType(exp.Expression.Type, ep);

            switch (exp.Expression.PropertyType)
            {
                case PropertyType.Value:
                    break;

                case PropertyType.Collection:
                case PropertyType.BasicCollection:

                    ep =
                        BuilderUtility.CallAction(
                            exp.Expression.Action, ep, BuildListCall(exp, ep, $"f{_peCounter}"));

                    _peCounter++;

                    break;

                case PropertyType.Key:

                    if (exp.Expression.Action == Action.Compare)
                        break;

                    if (exp.Expression.Action != Action.Contains)
                        throw new NotSupportedException();

                    ep = BuilderUtility.CallContains(exp, ep);

                    break;

                case PropertyType.Method:
                    break;

                case PropertyType.StringContains:

                    ep = BuilderUtility.CallStringContains(exp, ep);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (exp.Expression.Operator)
            {
                case Operator.Eq:
                    ex = Expression.Equal(ep, Expression.Convert(ce, memberType));
                    break;

                case Operator.Neq:
                    ex = Expression.NotEqual(ep, Expression.Convert(ce, memberType));
                    break;

                case Operator.Gt:
                    ex = Expression.GreaterThan(ep, Expression.Convert(ce, memberType));
                    break;

                case Operator.Gte:
                    ex = Expression.GreaterThanOrEqual(ep, Expression.Convert(ce, memberType));
                    break;

                case Operator.Lt:
                    ex = Expression.LessThan(ep, Expression.Convert(ce, memberType));
                    break;

                case Operator.Lte:
                    ex = Expression.LessThanOrEqual(ep, Expression.Convert(ce, memberType));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return ex;
        }

        private LambdaExpression BuildListCall(ExpressionData exp, Expression ep, string peParam)
        {
            var pi = ((MemberExpression) ep).Member as PropertyInfo;

            if (pi == null)
                throw new InvalidOperationException();

            var type = pi.PropertyType.GenericTypeArguments[0];
            var pe = Expression.Parameter(type, peParam);
            var json = JsonConvert.SerializeObject(exp.Expression.Data);
            var criteria = JsonConvert.DeserializeObject<Filter>(json);
            var be = BoolExpression(criteria, null, pe);
            var generic = typeof(Func<,>).MakeGenericType(type, typeof(bool));

            return Expression.Lambda(generic, be, pe);
        }
    }
}