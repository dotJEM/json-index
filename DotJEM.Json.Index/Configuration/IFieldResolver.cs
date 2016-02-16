using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public interface IFieldResolver
    {
        string Resolve(JObject entity);
    }

    public class FieldResolver : IFieldResolver
    {
        private readonly string field;

        public FieldResolver(string field)
        {
            this.field = field;
        }

        public string Resolve(JObject entity)
        {
            try
            {
                return entity[field].Value<string>();
            }
            catch (Exception)
            {
                
                throw;
            }
        }
    }
}