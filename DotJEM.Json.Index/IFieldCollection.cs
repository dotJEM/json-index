using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index
{
    public interface IFieldCollection
    {
        void Add(string contentType, string name, bool isIndexed);
        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);
    }

    public class FieldCollection : IFieldCollection
    {
        private readonly IDictionary<string, HashSet<string>> fieldMap = new Dictionary<string, HashSet<string>>();

        public void Add(string contentType, string name, bool isIndexed)
        {
            //TODO: Track non-indexed fields?... If so we should be able to filter them out when getting all fields.
            if(!isIndexed)
                return;
            
            if (!fieldMap.ContainsKey(contentType))
                fieldMap[contentType] = new HashSet<string>();
            fieldMap[contentType].Add(name);
        }

        public IEnumerable<string> AllFields()
        {
            return fieldMap.SelectMany(map => map.Value).Distinct();
        }

        public IEnumerable<string> Fields(string contentType)
        {
            return !fieldMap.ContainsKey(contentType) ? Enumerable.Empty<string>() : fieldMap[contentType];
        }
    }
}