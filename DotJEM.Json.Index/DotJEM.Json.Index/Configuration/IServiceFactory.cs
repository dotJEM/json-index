using System;
using System.Linq;
using System.Reflection;

namespace DotJEM.Json.Index.Configuration
{
    public interface IServiceFactory
    {
        object Create(IServiceResolver resolver, Type type);
    }

    public class DefaultServiceFactory : IServiceFactory
    {
        public object Create(IServiceResolver resolver, Type type)
        {
            (ConstructorInfo ctor, ParameterInfo[] parameters) = type
                .GetConstructors()
                .Select(c => new { Info = c, Params = c.GetParameters() })
                .OrderByDescending(c => c.Params.Length)
                .Where(c => c.Params.All(p => p.HasDefaultValue || resolver.Contains(p.ParameterType) || IsFactory(p.ParameterType)))
                .Select(c => (c.Info, c.Params))
                .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException();

            return ctor
                .Invoke(parameters.Select(p => (resolver.Contains(p.ParameterType) || !p.HasDefaultValue) ? resolver.Resolve(p.ParameterType) : p.DefaultValue).ToArray());

            bool IsFactory(Type paramType)
            {
                if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(IFactory<>))
                {
                    return resolver.Contains(paramType.GetGenericArguments().Single());
                }
                return false;
            }
        }
    }
}