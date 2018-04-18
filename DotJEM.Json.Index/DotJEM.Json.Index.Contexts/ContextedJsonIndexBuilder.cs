using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Contexts
{
    public class ContextedJsonIndexBuilder : JsonIndexBuilder
    {
        public ContextedJsonIndexBuilder(string name)
            : this(name, ServiceCollection.CreateDefault())
        {
        }
        public ContextedJsonIndexBuilder(string name,IServiceCollection services)
            : base(name, new PerIndexServiceCollection(services))
        {
        }
    }
}