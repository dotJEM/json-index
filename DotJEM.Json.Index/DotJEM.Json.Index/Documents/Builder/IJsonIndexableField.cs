using System;
using System.Collections.Generic;
using System.Linq;
using J2N.Collections.ObjectModel;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IIndexableJsonField
    {
        string SourcePath { get; }
        Type Strategy { get; }
        Type SourceType { get; }
        JTokenType TokenType { get; }
        IReadOnlyList<IIndexableField> LuceneFields { get; }
    }

    public class IndexableJsonField<T> : IIndexableJsonField
    {
        public string SourcePath { get; }
        public Type Strategy { get; }
        public Type SourceType { get; } = typeof(T);
        public JTokenType TokenType { get; }

        public IReadOnlyList<IIndexableField> LuceneFields { get; }

        public IndexableJsonField(string sourcePath, JTokenType tokenType, IIndexableField field, Type strategy)
            : this(sourcePath, tokenType, new [] { field }, strategy)
        {
        }

        public IndexableJsonField(string sourcePath, JTokenType tokenType, IEnumerable<IIndexableField> fields, Type strategy)
        {
            SourcePath = sourcePath;
            Strategy = strategy;
            TokenType = tokenType;
            LuceneFields = new ReadOnlyList<IIndexableField>(fields.ToList());
        }
    }

    public static class IndexableJsonFieldExt
    {
        public static IIndexableJsonFieldInfo Info(this IIndexableJsonField field)
        {
            return new IndexableJsonFieldInfo(field.SourcePath, field.TokenType, field.SourceType, field.Strategy, field.LuceneFields.Select(f => f.Info()));
        }

        public static IIndexableFieldInfo Info(this IIndexableField field)
        {
            return new IndexableFieldInfo(field.Name, field.GetType(), field.IndexableFieldType);
        }
    }

    public interface IContentTypeInfo
    {
        string Name { get; }

        //IContentTypeInfo Merge(IContentTypeInfo other);
        //IIndexableJsonFieldInfo Lookup(string fieldName);
        void Add(IIndexableJsonFieldInfo field);
    }

    public class ContentTypeInfo : IContentTypeInfo
    {
        private Dictionary<string, IIndexableJsonFieldInfo> fields
            = new Dictionary<string, IIndexableJsonFieldInfo>();
        
        private Dictionary<string, string> indexedFields
            = new Dictionary<string, string>();

        public string Name { get; }

        public ContentTypeInfo(string name)
        {
            Name = name;
        }

        public void Add(IIndexableJsonFieldInfo field)
        {
            lock (fields)
            {
                if (!fields.TryGetValue(field.SourcePath, out IIndexableJsonFieldInfo existing))
                    fields.Add(field.SourcePath, field);
                else
                {
                }
            }

            lock (indexedFields)
            {
                foreach (IIndexableFieldInfo info in field.LuceneFieldInfos)
                {
                    indexedFields[info.FieldName] = field.SourcePath;
                }
            }
        }
    }

    public interface IIndexableJsonFieldInfo
    {
        string SourcePath { get; }
        Type SourceType { get; }
        Type Strategy { get; }
        JTokenType TokenType { get; }

        IEnumerable<IIndexableFieldInfo> LuceneFieldInfos { get; }
    }

    public sealed class IndexableJsonFieldInfo : IIndexableJsonFieldInfo
    {
        public string SourcePath { get; }
        public Type SourceType { get; }
        public Type Strategy { get; }
        public JTokenType TokenType { get; }
        public IEnumerable<IIndexableFieldInfo> LuceneFieldInfos { get; }

        public IndexableJsonFieldInfo(string sourcePath, JTokenType tokenType, Type sourceType, Type strategy, IEnumerable<IIndexableFieldInfo> luceneFieldInfos)
        {
            SourcePath = sourcePath;
            TokenType = tokenType;
            SourceType = sourceType;
            Strategy = strategy;
            LuceneFieldInfos = luceneFieldInfos;
        }
    }

    public interface IIndexableFieldInfo
    {
        string FieldName { get; }
        Type Type { get; }
        IIndexableFieldType FieldType { get;  }
    }

    public sealed class IndexableFieldInfo : IIndexableFieldInfo
    {
        public string FieldName { get; }
        public Type Type { get; }
        public IIndexableFieldType FieldType { get; }

        public IndexableFieldInfo(string fieldName, Type type, IIndexableFieldType fieldType)
        {
            FieldName = fieldName;
            Type = type;
            FieldType = fieldType;
        }
    }
}