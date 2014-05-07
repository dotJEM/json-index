using System;
using System.Linq.Expressions;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Util
{
    public static class ConstraintExpressionExtensions
    {
        /// <summary/>
        public static ResolvableConstraintExpression Property<T>(this ConstraintExpression self, Expression<Func<T, object>> property)
        {
            return self.Property(property.GetPropertyInfo().Name);
        }

        /// <summary/>
        public static Constraint Matches<T>(this ConstraintExpression self, Predicate<T> predicate)
        {
            return self.Matches(predicate);
        }

        /// <summary/>
        public static Constraint That(this ConstraintExpression self, IResolveConstraint constraint)
        {
            return self.Matches(constraint.Resolve());
        }
    }
}