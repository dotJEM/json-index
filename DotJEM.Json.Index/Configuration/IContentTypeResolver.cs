using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public interface IContentTypeResolver
    {
        string Resolve(JObject entity);
    }

    public class FieldContentTypeResolver : IContentTypeResolver
    {
        private readonly string field;

        public FieldContentTypeResolver(string field)
        {
            this.field = field;
        }

        public string Resolve(JObject entity)
        {
            return entity[field].Value<string>();
        }
    }
}