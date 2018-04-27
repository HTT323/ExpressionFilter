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
    internal static class BuilderUtility
    {
        public static ConstantExpression BuildConstantExpression(
            IDictionary<string, IToken> tokens,
            Expression property,
            ExpressionData exp,
            DataType dataType)
        {
            ConstantExpression ce;

            if (exp.Expression.Right == null && string.IsNullOrWhiteSpace(exp.Expression.Token))
            {
                if (Nullable.GetUnderlyingType(property.Type) != null || dataType == DataType.String)
                {
                    return Expression.Constant(null);
                }

                throw new InvalidOperationException("Constant value or token is missing");
            }

            if (exp.Expression.Right != null && !string.IsNullOrWhiteSpace(exp.Expression.Token))
                throw new InvalidOperationException("Must specify a constant value or token but not both");

            if (!string.IsNullOrWhiteSpace(exp.Expression.Token) && !tokens.ContainsKey(exp.Expression.Token))
                throw new InvalidOperationException("Unable to find token from token collection");

            switch (dataType)
            {
                case DataType.Expression:
                    throw new InvalidOperationException();

                case DataType.Integer:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToInt32(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<int>(), typeof(int));

                    break;

                case DataType.String:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToString(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<string>(), typeof(string));

                    break;

                case DataType.Decimal:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToDecimal(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<decimal>(), typeof(decimal));

                    break;

                case DataType.Double:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToDouble(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<double>(), typeof(double));

                    break;

                case DataType.Boolean:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToBoolean(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<bool>(), typeof(bool));

                    break;

                case DataType.DateTime:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ToDateTime(exp.Expression.Right)
                            : tokens[exp.Expression.Token].Value<DateTime>(), typeof(DateTime));

                    break;

                case DataType.Guid:

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Guid.Parse(Convert.ToString(exp.Expression.Right))
                            : Guid.Parse(tokens[exp.Expression.Token].Value<string>()), typeof(Guid));

                    break;

                case DataType.Enum:

                    var type = Nullable.GetUnderlyingType(property.Type);
                    var enumType = Enum.GetUnderlyingType(type ?? property.Type);

                    ce = Expression.Constant(
                        exp.Expression.Right != null
                            ? Convert.ChangeType(exp.Expression.Right, enumType)
                            : Convert.ChangeType(tokens[exp.Expression.Token].Value<int>(), enumType));

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }

            return ce;
        }

        public static Expression GetProperty(Expression parameterExpression, string propertyId)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return parameterExpression;

            if (!propertyId.Contains('.'))
                return Expression.Property(parameterExpression, propertyId);

            var properties = propertyId.Split('.');
            var property = Expression.Property(parameterExpression, properties[0]);

            return properties.Skip(1).Aggregate(property, Expression.Property);
        }

        public static Expression CallAction(Action methodName, Expression property, Expression predicate)
        {
            var cType = GetIEnumerable(property.Type);
            var elemType = cType.GetGenericArguments()[0];
            var predType = typeof(Func<,>).MakeGenericType(elemType, typeof(bool));

            var action = (MethodInfo)
                GetGenericMethod(typeof(Enumerable), methodName.ToString(), new[] {elemType},
                    new[] {cType, predType}, BindingFlags.Static);

            property = Expression.Convert(property, cType);

            return Expression.Call(action, property, predicate);
        }

        public static Expression CallContains(ExpressionData exp, Expression ep)
        {
            Expression property = BuildContains(exp, ep);

            var cType = GetIEnumerable(property.Type);
            var elemType = cType.GetGenericArguments()[0];

            var action = (MethodInfo)
                GetGenericMethod(typeof(Enumerable), Action.Contains.ToString(), new[] {elemType},
                    new[] {cType, elemType}, BindingFlags.Static);

            property = Expression.Convert(property, cType);

            return Expression.Call(action, property, ep);
        }

        public static Expression CallStringContains(ExpressionData exp, Expression ep)
        {
            var type = typeof(string);
            var mi = type.GetMethod("Contains");

            if (mi == null)
                throw new InvalidOperationException();

            return Expression.Call(ep, mi, Expression.Constant(exp.Expression.Data, typeof(string)));
        }

        public static Expression CallMethod(
            Expression parameterExpression,
            ExpressionData exp,
            IDictionary<string, IMethod> methods)
        {
            if (!methods.ContainsKey(exp.Expression.PropertyId))
                throw new InvalidOperationException("Unable to find method from method collection");

            var instance = methods[exp.Expression.PropertyId];
            var type = instance.GetType();
            var constant = Expression.Constant(instance, type);

            var mi = (MethodInfo)
                GetGenericMethod(typeof(IMethod), "Evaluate", new[] {parameterExpression.Type},
                    new[] {parameterExpression.Type}, BindingFlags.Instance);

            return Expression.Call(constant, mi, parameterExpression);
        }

        public static Type GetDataType(DataType? type, Expression property)
        {
            switch (type)
            {
                case DataType.Integer:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(int?) : typeof(int);

                case DataType.String:
                    return typeof(string);

                case DataType.Decimal:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(decimal?) : typeof(decimal);

                case DataType.Double:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(double?) : typeof(double);

                case DataType.Boolean:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(bool?) : typeof(bool);

                case DataType.DateTime:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(DateTime?) : typeof(DateTime);

                case DataType.Guid:
                    return Nullable.GetUnderlyingType(property.Type) != null ? typeof(Guid?) : typeof(Guid);

                case DataType.Enum:
                    return property.Type;

                case DataType.Expression:
                case null:
                    throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Helpers

        private static ConstantExpression BuildContains(ExpressionData exp, Expression ep)
        {
            Type dataType;

            switch (exp.Expression.DataType)
            {
                case DataType.Integer:
                    dataType = typeof(int);
                    break;

                case DataType.String:
                    dataType = typeof(string);
                    break;

                case DataType.Decimal:
                    dataType = typeof(decimal);
                    break;

                case DataType.Double:
                    dataType = typeof(double);
                    break;

                case DataType.Boolean:
                    dataType = typeof(bool);
                    break;

                case DataType.DateTime:
                    dataType = typeof(DateTime);
                    break;

                case DataType.Guid:
                    dataType = typeof(Guid);
                    break;

                case DataType.Enum:
                    dataType = ep.Type;
                    break;

                case DataType.Expression:
                case null:
                    throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var generic = typeof(IEnumerable<>).MakeGenericType(dataType);
            var data = JsonConvert.SerializeObject(exp.Expression.Data);
            var typedData = JsonConvert.DeserializeObject(data, generic);

            return Expression.Constant(typedData);
        }

        private static MethodBase GetGenericMethod(
            Type type,
            string name,
            Type[] typeArgs,
            Type[] argTypes,
            BindingFlags flags)
        {
            var typeArity = typeArgs.Length;

            var methods =
                type.GetMethods()
                    .Where(f => f.Name == name)
                    .Where(f => f.GetGenericArguments().Length == typeArity)
                    .Select(s => s.MakeGenericMethod(typeArgs));

            if (Type.DefaultBinder == null)
                throw new InvalidOperationException();

            return Type.DefaultBinder.SelectMethod(flags, methods.ToArray<MethodBase>(), argTypes, null);
        }

        private static Type GetIEnumerable(Type type)
        {
            if (IsIEnumerable(type))
                return type;

            var t = type.FindInterfaces((m, o) => IsIEnumerable(m), null);

            Debug.Assert(t.Length == 1);

            return t[0];
        }

        private static bool IsIEnumerable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        #endregion
    }
}