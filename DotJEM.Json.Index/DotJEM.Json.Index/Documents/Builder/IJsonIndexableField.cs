using System;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.Documents.Strategies;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IJsonIndexableField
    {
        string FieldName { get; }
        Type ClrType { get; }
        FieldType LuceneType { get; }
        FieldMetaData[] Data { get; }
        IIndexableField Field { get; }
    }

    public class JsonIndexableField<T> : IJsonIndexableField
    {
        public string FieldName { get; }
        public Type ClrType { get; } = typeof(T);
        public FieldType LuceneType { get; }
        public FieldMetaData[] Data { get; }

        public IIndexableField Field { get; }

        public JsonIndexableField(Field field, params FieldMetaData[] data)
        {
            Field = field;
            LuceneType = field.FieldType;
            FieldName = field.Name;
            Data = data;
        }

    }

}