using System;
using System.Linq.Expressions;
using DotJEM.Json.Index.Test.Constraints.Properties;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints
{
    public class IS
    {
        public static IResolveConstraint EqualToJson(object expected)
        {
            string str = expected as string;
            if (str != null)
            {
                return new JEqualsContraint(JToken.Parse(str));
            }
            JToken jobj = expected as JToken ?? JToken.FromObject(expected);
            return new JEqualsContraint(jobj);

        }

        //JTokenEqualityComparer
    }

    public class JEqualsContraint : AbstractConstraint
    {
        private readonly JToken expectedToken;

        public JEqualsContraint(JToken expectedToken)
        {
            this.expectedToken = expectedToken;
        }

        protected override void DoMatches(object actual)
        {
            JToken actualToken = actual as JToken;
            if (actualToken == null)
            {
                FailWithMessage("Object was not a JToken");
                return;
            }

            if (!JToken.DeepEquals(actualToken, expectedToken))
            {
                FailWithMessage("Expected JTokens to equal, they did not:");
                FailWithMessage("actual:   " + actualToken);
                FailWithMessage("expected: " + expectedToken);
            }
        }

    }

    public class XIS
    {
        public static IResolveConstraint JsonEqual(object expected)
        {
            JObject jobj;
            string str = expected as string;
            if (str != null)
            {
                jobj = JObject.Parse(str);
            }
            else
            {
                jobj = expected as JObject ?? JObject.FromObject(expected);
            }
            //Note: If expected was not a JObject, Most likely used with an anonomous type...
            //      but this also means we can allow for actual business objects to be passed in directly.
            return new JsonEqualsConstraint(jobj);
        }
    }

    public class HAS
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
            JObject jobj;
            string str = expected as string;
            if (str != null)
            {
                jobj = JObject.Parse(str);
            }
            else
            {
                jobj = expected as JObject ?? JObject.FromObject(expected);
            }
            //Note: If expected was not a JObject, Most likely used with an anonomous type...
            //      but this also means we can allow for actual business objects to be passed in directly.
            return new HasJsonPropertiesConstraint(jobj);
        }
    }
}