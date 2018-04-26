using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Contexts.Configuration
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