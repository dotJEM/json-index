using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints.Properties
{
    public class ObjectPropertiesNotEqualsConstraint<T> : ObjectPropertiesEqualsConstraint<T>
    {
        private bool strict;

        #region Ctor

        public ObjectPropertiesNotEqualsConstraint(T expected) : base(expected)
        {
        }

        public ObjectPropertiesNotEqualsConstraint(T expected, HashSet<object> references) : base(expected, references)
        {
        }

        #endregion

        #region Initialise

        protected override Constraint SetupPrimitive(object expected)
        {
            return Is.Not.EqualTo(expected);
        }

        #endregion

        #region Constraint members

        public override bool Matches(object actualObject)
        {
            if(base.Matches(actualObject))
                return false;

            if(strict)
            {
                foreach (Property property in propertyMap.Values)
                {
                    try
                    {
                        property.Actual = property.Info.GetValue(actual, null);
                        property.Expected = property.Info.GetValue(Expected, null);
                        if (property.Matches)
                            return false;
                    }
                    catch (TargetException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        /// <summary>
        /// Checks all properties and if any one of them matches the constraint fails.
        /// </summary>
        public ObjectPropertiesNotEqualsConstraint<T> Strict()
        {
            strict = true;
            return this;
        }
    }
}
