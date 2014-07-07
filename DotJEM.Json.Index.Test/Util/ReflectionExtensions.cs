using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace DotJEM.Json.Index.Test.Util
{
    public static class ReflectionExtensions
    {
        /// <summary/>
        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
            {
                UnaryExpression convert = propertyLambda.Body as UnaryExpression;
                if (convert != null && convert.NodeType == ExpressionType.Convert)
                    member = convert.Operand as MemberExpression;
                if (member == null)
                {
                    throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", propertyLambda));
                }
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", propertyLambda));

            Debug.Assert(propInfo.ReflectedType != null, "propInfo.ReflectedType != null");
            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format("Expresion '{0}' refers to a property that is not from type {1}.", propertyLambda, type));

            return propInfo;
        }

    }
}