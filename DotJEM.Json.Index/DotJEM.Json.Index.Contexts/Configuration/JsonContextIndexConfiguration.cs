using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Contexts
{
    public class JsonContextIndexConfiguration : JsonIndexConfiguration
    {
        public IJsonIndexConfiguration ContextConfiguration { get; }

        public JsonContextIndexConfiguration(IJsonIndexConfiguration contextConfiguration)
        {
            ContextConfiguration = contextConfiguration;
        }
    }
}