using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotJEM.Json.Index.Test.Util;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DotJEM.Json.Index.Test.Constraints.Properties
{
    public class ReferenceComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }
    }

    public class ObjectPropertiesEqualsConstraint<T> : Constraint
    {
        #region Fields

        protected Constraint primitive;
        protected readonly Dictionary<string, Property> propertyMap = new Dictionary<string, Property>();

        private readonly HashSet<object> references = new HashSet<object>(/*new ReferenceComparer()*/);

        #endregion

        #region Properties

        protected T Expected { get; private set; }
        protected bool ExplicitTypesFlag { get; set; }

        #endregion

        #region Ctor

        public ObjectPropertiesEqualsConstraint(T expected)
        {
            Expected = expected;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            InitializeProperties();
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        public ObjectPropertiesEqualsConstraint(T expected, HashSet<object> references)
        {
            Expected = expected;
            this.references = references;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            InitializeProperties();
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        #endregion

        #region Initialize

        protected virtual void InitializeProperties()
        {
            if (ReferenceEquals(Expected, null))
            {
                primitive = SetupPrimitive(Expected);
                return;
            }

            Type type = Expected.GetType();
            PropertyInfo[] properties = type.GetProperties();

            if (properties.Length == 0)
                primitive = SetupPrimitive(Expected);

            //Note: No need for "Else", foreach automatically terminates on an empty collection.
            foreach (PropertyInfo property in properties.Where(property => property.GetIndexParameters().Length == 0))
            {
                object expectedObject = property.GetValue(Expected, null);
                if (references.Contains(expectedObject)) 
                    continue;
                
                references.Add(expectedObject);
                SetupProperty(property, property.GetValue(Expected, null));
            }
        }
        
        protected virtual Constraint SetupPrimitive(object expected)
        {
            return Is.EqualTo(expected);
        }
        
        protected void SetupProperty(Expression<Func<T, object>> property)
        {
            PropertyInfo propertyInfo = property.GetPropertyInfo();
            SetupProperty(propertyInfo, propertyInfo.GetValue(Expected, null));
        }
        
        protected void SetupProperty(Expression<Func<T, object>> property, Constraint constraint)
        {
            SetupProperty(property.GetPropertyInfo(), constraint);
        }

        protected void SetupProperty<T2>(Expression<Func<T, T2>> property, Func<T2, Constraint> constraintFactory)
        {
            SetupProperty(property.GetPropertyInfo(), constraintFactory);
        }

        #endregion

        #region Private Methods

        private void SetupProperty(PropertyInfo property, Constraint constraint)
        {
            propertyMap[property.Name] = new Property { Info = property, Constraint = constraint };
        }

        private void SetupProperty(PropertyInfo property, object expected)
        {
            Type type = property.PropertyType;
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                SetupProperty(property, Is.EqualTo(expected));
            }
            else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
            {
                SetupProperty(property, new EnumerableEqualsConstraint((IEnumerable)expected));
            }
            else
            {
                SetupProperty(property, new ObjectPropertiesEqualsConstraint<object>(expected, references));
            }
        }

        private void SetupConstraint(PropertyInfo property, Constraint constraint)
        {
            if (!propertyMap.ContainsKey(property.Name))
                throw new ArgumentException("Property was not valid, check that it has not been demoved using 'Ignore'.", "property");

            propertyMap[property.Name].Constraint = constraint;
            return;
        }

        #endregion

        #region Constraint Members

        public override bool Matches(object actualObject)
        {
            actual = actualObject;
            if (ExplicitTypesFlag && Object.ReferenceEquals(actual.GetType(), Expected.GetType()) == false)
                return false;

            bool matches = true;
            if (primitive != null)
                return primitive.Matches(actualObject);

            foreach (Property property in propertyMap.Values)
            {
                try
                {
                    property.Actual = property.Info.GetValue(actual, null);
                    property.Expected = property.Info.GetValue(Expected, null);
                    if (!property.Matches)
                        matches = false;
                }
                catch (TargetException ex)
                {
                    string message = string.Format("Actual ({1}) and Expected ({2}) was not of same type and Expected contained the property '{0}' which Actual did not.", property.Info.Name, actual.GetType().Name, Expected.GetType().Name);
                    throw new ArgumentException(message, ex);
                }
            }
            return matches;
        }

        public override void WriteMessageTo(MessageWriter writer)
        {
            if (ExplicitTypesFlag && Object.ReferenceEquals(actual.GetType(), Expected.GetType()) == false)
            {
                writer.WriteMessageLine("Expected object of type '{0}' but was '{1}'.", Expected.GetType(), actual.GetType());
                return;
            }

            if (primitive != null)
                primitive.WriteMessageTo(writer);

            foreach (var property in propertyMap.Values.Where(property => !property.Matches))
            {
                writer.WriteMessageLine(-1, "The property '{0} <{1}>' was not equal.", property.Info.Name, property.Info.PropertyType);
                property.Constraint.WriteMessageTo(writer);
                writer.WriteLine();
            }
        }

        public override void WriteDescriptionTo(MessageWriter writer)
        {
        }

        #endregion

        #region Modifyer Methods Methods

        /// <summary>
        /// Ignores the specified properties from the comparison of two objects.
        /// </summary>
        /// <param name="properties">The properties to ignore.</param>
        public ObjectPropertiesEqualsConstraint<T> Ignore(params Expression<Func<T, object>>[] properties)
        {                                                   
            foreach (PropertyInfo info in properties.Select(ReflectionExtensions.GetPropertyInfo))
                propertyMap.Remove(info.Name);
            return this;
        }


        /// <summary>
        /// Uses the specified Constraint to compare the specified property or properties.
        /// </summary>
        public ObjectPropertiesEqualsConstraint<T> CheckTypes()
        {
            ExplicitTypesFlag = true;
            foreach (Property property in propertyMap.Values)
            {
                try
                {
                    (property.Constraint as dynamic).ExplicitTypesFlag = true;
                }
                catch (RuntimeBinderException)
                {
                }
            }
            return this;
        }

        /// <summary>
        /// Gets a Modifyer that provides the ability to modify the comparison for all properties.
        /// </summary>
        public ObjectPropertyEqualsModifyer<object> ForAll
        {
            get { return new ObjectPropertyEqualsModifyer<object>(this, propertyMap.Values.Select(f => f.Info).ToArray()); }
        }

        /// <summary>
        /// Modifies a single property using the returned modifyer.
        /// </summary>
        public ObjectPropertyEqualsModifyer<TProperty> For<TProperty>(params Expression<Func<T, TProperty>>[] properties)
        {
            return new ObjectPropertyEqualsModifyer<TProperty>(this, properties.Select(ReflectionExtensions.GetPropertyInfo).ToArray());
        }

        #endregion

        #region Nested Types

        protected class Property
        {
            public bool Matches { get { return Constraint.Matches(Actual); } }
            public PropertyInfo Info { get; set; }
            public object Expected { get; set; }
            public object Actual { get; set; }
            public Constraint Constraint { get; set; }
        }

        public class ObjectPropertyEqualsModifyer<TProperty>
        {
            private readonly PropertyInfo[] properties;
            private readonly ObjectPropertiesEqualsConstraint<T> parrent;

            internal ObjectPropertyEqualsModifyer(ObjectPropertiesEqualsConstraint<T> parrent, PropertyInfo[] properties)
            {
                this.properties = properties;
                this.parrent = parrent;
            }

            /// <summary>
            /// Uses the specified Comparison to compare the specified property or properties.
            /// </summary>
            public ObjectPropertiesEqualsConstraint<T> Use(Comparison<TProperty> comparison)
            {
                foreach (PropertyInfo property in properties)
                    SetupProperty(property, Is.EqualTo(property.GetValue(parrent.Expected, null)).Using(comparison));
                return parrent;
            }

            /// <summary>
            /// Uses the specified Constraint to compare the specified property or properties.
            /// </summary>
            public ObjectPropertiesEqualsConstraint<T> Use(Func<TProperty, Constraint> constraintFactory)
            {
                foreach (PropertyInfo property in properties)
                    SetupProperty(property, constraintFactory((TProperty)property.GetValue(parrent.Expected, null)));
                return parrent;
            }

            /// <summary>
            /// Uses the specified Constraint to compare the specified property or properties.
            /// </summary>
            public ObjectPropertiesEqualsConstraint<T> Use<TConstraint>(Func<TProperty, TConstraint> constraintFactory, Func<TConstraint, TConstraint> modifyer) where TConstraint : Constraint
            {
                foreach (PropertyInfo property in properties)
                {
                    SetupProperty(property, modifyer(constraintFactory((TProperty)property.GetValue(parrent.Expected, null))));
                }
                return parrent;
            }

            /// <summary>
            /// Uses the specified Constraint to compare the specified property or properties.
            /// </summary>
            public ObjectPropertiesEqualsConstraint<T> Use(Constraint constraint)
            {
                foreach (PropertyInfo property in properties)
                    SetupProperty(property, constraint);
                return parrent;
            }

            private void SetupProperty(PropertyInfo property, Constraint constraint)
            {
                parrent.SetupConstraint(property, constraint);
            }
        }
        #endregion
    }
}