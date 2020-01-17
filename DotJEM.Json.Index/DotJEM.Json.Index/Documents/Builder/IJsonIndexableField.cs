using System;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.Documents.Strategies;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IJsonIndexableField
    {
        string FieldName { get; }
        Type ClrType { get; }
        FieldType LuceneType { get; }
        JObject MetaData { get; }
        IIndexableField Field { get; }
    }

    public class JsonIndexableField<T> : IJsonIndexableField
    {
        public string FieldName { get; }
        public Type ClrType { get; } = typeof(T);
        public FieldType LuceneType { get; }
        public JObject MetaData { get; }

        public IIndexableField Field { get; }

        public JsonIndexableField(Field field, JObject metaData = null)
        {
            Field = field;
            LuceneType = field.FieldType;
            FieldName = field.Name;
            MetaData = metaData;
        }

    }

}