using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IdentityStrategies
{
    public interface IIdentityResolver
    {
        string Resolve(JObject entity);
        Term CreateTerm(JObject entity);
    }
}