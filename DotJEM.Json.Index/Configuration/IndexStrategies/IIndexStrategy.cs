using System.Collections.Generic;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IndexStrategies
{
    public interface IIndexStrategy
    {
        IEnumerable<IFieldable> CreateField(string fieldName, JValue value);
    }
}