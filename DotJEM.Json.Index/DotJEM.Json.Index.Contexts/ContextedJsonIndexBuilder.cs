using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Contexts.Configuration;

namespace DotJEM.Json.Index.Contexts
{
    public class ContextedLuceneJsonIndexBuilder : LuceneJsonIndexBuilder
    {
        public ContextedLuceneJsonIndexBuilder(string name)
            : this(name, ServiceCollection.CreateDefault())
        {
        }
        public ContextedLuceneJsonIndexBuilder(string name,IServiceCollection services)
            : base(name, new PerIndexServiceCollection(services))
        {
        }
    }
}