using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IdentityStrategies
{
    public class FieldIdentityStrategy : IIdentityStrategy
    {
        private readonly string field;

        public FieldIdentityStrategy(string field)
        {
            this.field = field;
        }

        public string Resolve(JObject entity)
        {
            return entity[field].Value<string>();
        }

        public Term CreateTerm(JObject entity)
        {
            return new Term(field, Resolve(entity));
        }
    }
}