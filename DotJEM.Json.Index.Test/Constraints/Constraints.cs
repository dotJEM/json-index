using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints
{
    /// <summary/>
    public abstract class AbstractConstraint : Constraint
    {
        private bool result = true;
        private StringBuilder message;

        #region Core
        public override bool Matches(object actual)
        {
            try
            {
                base.actual = actual;
                message = new StringBuilder();
                DoMatches(actual);
                return result;
            }
            finally
            {
                result = true;
            }
        }

        protected abstract void DoMatches(object actual);

        /// <summary/>
        public override void WriteMessageTo(MessageWriter writer)
        {
            writer.WriteMessageLine(GetType().FullName + " Failed!");
            using (StringReader reader = new StringReader(message.ToString()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    writer.WriteMessageLine(line);
            }
        }

        /// <summary/>
        public override void WriteDescriptionTo(MessageWriter writer)
        {
        }
        #endregion

        /// <summary/>
        protected bool FailWithMessage(string format, params object[] args)
        {
            AppendLine(string.Format(format, args));
            return Fail();
        }

        /// <summary/>
        protected bool Fail()
        {
            return result = false;
        }

        /// <summary/>
        protected StringBuilder AppendFormat(string format, params object[] args)
        {
            return message.AppendFormat(format, args);
        }

        /// <summary/>
        protected StringBuilder AppendLine(string value)
        {
            return message.AppendLine(value);
        }

        /// <summary/>
        protected StringBuilder AppendLine()
        {
            return message.AppendLine();
        }
    }

    public class HAS
    {
        public static ResolvableConstraintExpression Property<T>(Expression<Func<T, object>> property)
        {
            return new ConstraintExpression().Property(property.GetPropertyInfo().Name);
        }

        public static IResolveConstraint JProperties(object expected)
        {
            //Note: If expected was not a JObject, Most likely used with an anonomous type...
            //      but this also means we can allow for actual business objects to be passed in directly.
            JObject jobj = expected as JObject ?? JObject.FromObject(expected);
            return new HasJsonPropertiesConstraint(jobj);
        }
    }

    public class HasJsonPropertiesConstraint : AbstractConstraint
    {
        private readonly JObject expectedJObject;

        public HasJsonPropertiesConstraint(JObject expectedJObject)
        {
            this.expectedJObject = expectedJObject;
        }

        protected override void DoMatches(object actual)
        {
            JObject actualJObject = actual as JObject;
            if (actualJObject == null)
            {
                FailWithMessage("Object was not a JToken");
                return;
            }

            CompareJObjects(expectedJObject, actualJObject);
        }

        private void CompareJObjects(JObject expected, JObject actual)
        {
            foreach (JProperty expectedProperty in expected.Properties())
            {
                JProperty actualProperty = actual.Property(expectedProperty.Name);
                if (actualProperty == null)
                {
                    FailWithMessage("Actual object did not contain a property named '{0}'", expectedProperty.Name);
                    continue;
                }

                if (actualProperty.Value.Type != expectedProperty.Value.Type)
                {
                    FailWithMessage("Property named '{0}' was expected to be of type '{1}' but was of type '{2}'.",
                        expectedProperty.Name, expectedProperty.Value.Type, actualProperty.Value.Type);
                    continue;
                }

                JObject obj = expectedProperty.Value as JObject;
                if (obj != null)
                {
                    //Note: We compared types above, so we know they should pass for both in this case.
                    CompareJObjects(obj, (JObject)actualProperty.Value);
                }

                JArray array = expectedProperty.Value as JArray;
                if (array != null)
                {
                    //Note: We compared types above, so we know they should pass for both in this case.
                    CompareJArray(array, (JArray)actualProperty.Value);
                }

                JValue value = expectedProperty.Value as JValue;
                if (value != null)
                {
                    //Note: We compared types above, so we know they should pass for both in this case.
                    if (!value.Equals((JValue)actualProperty.Value))
                    {
                        FailWithMessage("Property named '{0}' was expected to be '{1}' but was '{2}'.",
                            expectedProperty.Name, expectedProperty.Value, actualProperty.Value);
                    }
                }
            }
        }

        private void CompareJArray(JArray expected, JArray actual)
        {
            throw new NotImplementedException("Comparing arrays in JObjects are not yet implemented...");
        }
    }
}
