using System.Collections.Concurrent;

namespace DotJEM.Json.Index.Sharding.Configuration
{
    public interface IJsonIndexContextConfiguration
    {
        IJsonIndexConfiguration this[string name] { get; set; }
    }

    public class LuceneJsonIndexContextConfiguration : IJsonIndexContextConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get { return configurations.GetOrAdd(name, key => new DefaultJsonIndexConfiguration()); }
        }
    }

    public interface IJsonIndexConfiguration
    {
        IDocumentFactory DocumentFactory { get; }
    }

    public class DefaultJsonIndexConfiguration : IJsonIndexConfiguration
    {
        public IDocumentFactory DocumentFactory { get; }
    }
}