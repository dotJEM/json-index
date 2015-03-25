using System.Collections.Generic;
using System.Linq;
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
        private readonly IStorageIndex index;

        public FieldFactory(IStorageIndex index)
        {
            this.index = index;
        }

        public IEnumerable<IFieldable> Create(string fullName, string contentType, JValue value)
        {
            //TODO: Return FieldDefinitions???
            List<IFieldable> fields = index.Configuration.Field
                .Strategy(contentType, fullName)
                .CreateField(fullName, value).ToList();
            return fields;
        }
    }
}