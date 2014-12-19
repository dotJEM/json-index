using System;
using System.Reflection;

namespace DotJEM.Json.Index.Test.Constraints.Properties
{
    /// <summary>
    /// Contains helper methods for dealing with private content of classes for Unit test purpouses.
    /// </summary>
    public static class PrivateMembersHelper
    {
        private const BindingFlags ALL_PRIVATE_FLAGS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Sets a private property given by it's name on a specific object.
        /// </summary>
        /// <param name="target">The target object on which to set the property.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value to assign to the property.</param>
        /// <example>
        /// public void AsExtentionMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   myObjectOfMyClass.SetPrivateProperty("MyProperty", "MyValue");
        /// }
        /// 
        /// public void AsStaticMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   PrivateMembersHelper.SetPrivateProperty(myObjectOfMyClass, "MyProperty", "MyValue");
        /// }
        /// </example>
        public static void SetPrivateProperty(this object target, string name, object value)
        {
            PropertyInfo property = target.GetType().LocateProperty(name, true, false);
            property.SetValue(target, value, null);
        }

        /// <summary>
        /// Sets a private field given by it's name on a specific object.
        /// </summary>
        /// <param name="target">The target object on which to set the field.</param>
        /// <param name="name">The name of the field to set.</param>
        /// <param name="value">The value to assign to the field.</param>
        /// <example>
        /// public void AsExtentionMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   myObjectOfMyClass.SetPrivateField("myField", "MyValue");
        /// }
        /// 
        /// public void AsStaticMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   PrivateMembersHelper.SetPrivateField(myObjectOfMyClass, "myField", "MyValue");
        /// }
        /// </example>
        public static void SetPrivateField(this object target, string name, object value)
        {
            FieldInfo field = target.GetType().LocateField(name);
            field.SetValue(target, value);
        }

        /// <summary>
        /// Gets a private field given by it's name on a specific object.
        /// </summary>
        /// <typeparam name="T">Defines what type the method should cast the field value to before return</typeparam>
        /// <param name="target">The target object on which to get the field from.</param>
        /// <param name="name">The name of the field to get.</param>
        /// <returns></returns>
        /// <example>
        /// public void AsExtentionMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   string field = myObjectOfMyClass.GetPrivateField&lt;string&gt;("myField");
        /// }
        /// 
        /// public void AsStaticMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   string field = PrivateMembersHelper.GetPrivateField&lt;string&gt;(myObjectOfMyClass, "myField");
        /// }
        /// </example>
        public static T GetPrivateField<T>(this object target, string name)
        {
            FieldInfo field = target.GetType().LocateField(name);
            return (T) field.GetValue(target);
        }

        /// <summary>
        /// Gets a private field given by it's name on a specific object.
        /// </summary>
        /// <typeparam name="T">Defines what type the method should cast the field value to before return</typeparam>
        /// <param name="target">The target object on which to get the field from.</param>
        /// <param name="name">The name of the field to get.</param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// public void AsExtentionMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   string field = myObjectOfMyClass.GetPrivateProperty&lt;string&gt;("myField");
        /// }
        /// 
        /// public void AsStaticMethod()
        /// {
        ///   MyClass myObjectOfMyClass = new MyClass();
        ///   string field = PrivateMembersHelper.GetPrivateProperty&lt;string&gt;(myObjectOfMyClass, "myField");
        /// }
        /// </code>
        /// </example>
        public static T GetPrivateProperty<T>(this object target, string name)
        {
            PropertyInfo property = target.GetType().LocateProperty(name, true, false);
            return (T)property.GetValue(target, null);
        }

        #region Private Helpers.

        private static PropertyInfo LocateProperty(this Type targetType, string name, bool demandWrite, bool demandRead)
        {
            try
            {
                PropertyInfo propertyInfo = targetType.GetProperty(name, ALL_PRIVATE_FLAGS);

                if (targetType.BaseType != typeof(object))
                {
                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    if ((demandRead && demandWrite) && (!propertyInfo.CanWrite || !propertyInfo.CanRead))
                        return targetType.BaseType.LocateProperty(name, demandWrite, demandRead);

                    if (demandWrite && !propertyInfo.CanWrite)
                        return targetType.BaseType.LocateProperty(name, demandWrite, demandRead);

                    if (demandRead && !propertyInfo.CanRead)
                        return targetType.BaseType.LocateProperty(name, demandWrite, demandRead);
                    // ReSharper restore ConditionIsAlwaysTrueOrFalse
                }

                return propertyInfo;
            }
            catch (ArgumentException)
            {
                if (targetType.BaseType != typeof(object))
                {
                    return targetType.BaseType.LocateProperty(name, demandWrite, demandRead);
                }
                throw;
            }
        }

        private static FieldInfo LocateField(this Type targetType, string name)
        {
            try
            {
                FieldInfo fieldInfo = targetType.GetField(name, ALL_PRIVATE_FLAGS);
                if (fieldInfo == null && targetType.BaseType != typeof(object))
                    return targetType.BaseType.LocateField(name);
                return fieldInfo;
            }
            catch (ArgumentException)
            {
                if (targetType.BaseType != typeof(object))
                {
                    return targetType.BaseType.LocateField(name);
                }
                throw;
            }
        }

        #endregion
    }
}