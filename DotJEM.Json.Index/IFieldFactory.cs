using System.Collections.Generic;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IFieldFactory
    {
        IEnumerable<IFieldable> Create(string fullname, string contentType, JValue value);
    }

    public class FieldFactory : IFieldFactory
    {
        private readonly IIndexConfiguration configuration;

        public FieldFactory(IIndexConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<IFieldable> Create(string fullName, string contentType, JValue value)
        {
            return configuration.Index.Strategy(contentType, fullName).CreateField(fullName, value);
        }
    }
}