using System.Collections.Concurrent;
using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Contexts.Configuration
{
    public interface IJsonIndexConfigurationProvider
    {
        IJsonIndexConfiguration Acquire(string name);
        IJsonIndexConfigurationProvider Use(string name, IJsonIndexConfiguration config);
    }

    public class LuceneIndexConfigurationProvider : IJsonIndexConfigurationProvider
    {
        public IJsonIndexConfiguration Global { get; set; } = new JsonIndexConfiguration();

        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations
            = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration Acquire(string name)
            => configurations.GetOrAdd(name, s => new JsonContextIndexConfiguration(Global));

        public IJsonIndexConfigurationProvider Use(string name, IJsonIndexConfiguration config)
        {
            configurations[name] = config;
            return this;
        }
    }
}