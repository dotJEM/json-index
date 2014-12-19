using System;
using System.Linq.Expressions;
using DotJEM.Json.Index.Test.Constraints.Properties;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints
{
    public class HAS
        // ReSharper restore InconsistentNaming
    {
        public static ResolvableConstraintExpression Property<T>(Expression<Func<T, object>> property)
        {
            return new ConstraintExpression().Property(property.GetPropertyInfo().Name);
        }
        
        public static ResolvableConstraintExpression Property<TSource, TProperty>(Expression<Func<TSource, TProperty>> expression)
        {
            return new ConstraintExpression().Property(expression.GetPropertyInfo().Name);
        }

        public static IPropertiesConstraintsFactory Properties
        {
            get { return new PropertiesConstraintsFactory(); }
        }

        public static IResolveConstraint JProperties(object expected)
        {
            //Note: If expected was not a JObject, Most likely used with an anonomous type...
            //      but this also means we can allow for actual business objects to be passed in directly.
            JObject jobj = expected as JObject ?? JObject.FromObject(expected);
            return new HasJsonPropertiesConstraint(jobj);
        }
    }
}