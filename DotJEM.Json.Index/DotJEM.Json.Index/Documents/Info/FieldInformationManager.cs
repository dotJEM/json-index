using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Strategies;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Info
{
    public interface IFieldInformationManager
    {
        IInfoEventStream InfoStream { get; }
        IFieldResolver Resolver { get; }

        IEnumerable<string> ContentTypes { get; }
        IEnumerable<IJsonFieldInfo> AllFields { get; }

        //Task Merge(string contentType, JObject entity);
        void Merge(string contentType, IFieldInfoCollection info);

        IJsonFieldInfo Lookup(string fieldName);
        IJsonFieldInfo Lookup(string contentType, string fieldName);
    }

    //TODO: How can we make the abstraction work with JSchema as well as our own simple collector?
    public interface IFieldInfoCollection : IEnumerable<IJsonFieldInfo>
    {
        IFieldInfoCollection Merge(IFieldInfoCollection other);
        IJsonFieldInfo Lookup(string fieldName);
    }

    public class FieldInfoCollection : IFieldInfoCollection
    {
        private readonly Dictionary<string, IJsonFieldInfo> fields;

        public FieldInfoCollection() : this(new Dictionary<string, IJsonFieldInfo>()) { }

        public FieldInfoCollection(IEnumerable<IJsonFieldInfo> fields)
            : this(fields
                .GroupBy(info => info.Name)
                .Select(group => group.Aggregate((left, right) => left.Merge(right)))
                .ToDictionary(info => info.Name)) { }

        public FieldInfoCollection(IDictionary<string, IJsonFieldInfo> fields)
        {
            if(!(fields is Dictionary<string, IJsonFieldInfo> fieldsDictionary))
                fieldsDictionary = new Dictionary<string, IJsonFieldInfo>(fields);
            this.fields = fieldsDictionary;
        }

        public IFieldInfoCollection Merge(IFieldInfoCollection other)
        {
            Dictionary<string, IJsonFieldInfo> merged = new Dictionary<string, IJsonFieldInfo>(fields);
            foreach (IJsonFieldInfo info in other)
            {
                if (merged.TryGetValue(info.Name, out IJsonFieldInfo fieldInfo))
                {
                    merged[info.Name] = fieldInfo.Merge(info);
                }
                else
                {
                    merged[info.Name] = info;
                }
            }
            return new FieldInfoCollection(merged);
        }

        public IJsonFieldInfo Lookup(string fieldName)
        {
            return fields.TryGetValue(fieldName, out IJsonFieldInfo field) ? field : null;
        }

        public IEnumerator<IJsonFieldInfo> GetEnumerator() => fields.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
   public interface IJsonFieldInfo : IEnumerable<ILuceneFieldInfo>
    {
        string Name { get; }
        JTokenType TokenType { get; }
        IEnumerable<Type> ClrType { get; }
    }

   public static class JsonFieldInfoExtensions
   {
       public static IJsonFieldInfo Merge(this IJsonFieldInfo self, IJsonFieldInfo other)
       {
           return null;
       }
   }


    public interface ILuceneFieldInfo
    {
        string LuceneField { get; }
        FieldType LuceneType { get; }
        Type StrategyType { get; }
        JObject MetaData { get; }
    }

    public class LuceneFieldInfo : ILuceneFieldInfo
    {
        public string Key { get; }
        public string LuceneField { get; }
        public FieldType LuceneType { get; }
        public Type StrategyType { get; }
        public JObject MetaData { get; }

        public LuceneFieldInfo(string luceneField, JTokenType jsonType, Type clrType, FieldType luceneType, Type strategyType, JObject metaData)
        {
            LuceneField = luceneField;
            LuceneType = luceneType;
            StrategyType = strategyType;
            MetaData = metaData;
            Key =
                $"{luceneField};{(int)luceneType.DocValueType};{(int)luceneType.IndexOptions};{(int)luceneType.IndexOptions}" +
                $";{(int)luceneType.DocValueType};" +
                $"{luceneType.GetBase64Flags()};{luceneType.NumericPrecisionStep};{(int)jsonType};{clrType};{strategyType}";
        }
    }

    
    public static class FieldTypeExtensions
    {
        public static LuceneFieldFlags GetFlags(this FieldType field)
        {
            LuceneFieldFlags flags = LuceneFieldFlags.None;
            if (field.IsIndexed)
                flags |= LuceneFieldFlags.Indexed;
            if (field.IsStored)
                flags |= LuceneFieldFlags.Stored;
            if (field.IsTokenized)
                flags |= LuceneFieldFlags.Tokenized;
            if (field.OmitNorms)
                flags |= LuceneFieldFlags.OmitNorms;
            if (field.StoreTermVectorOffsets)
                flags |= LuceneFieldFlags.StoreTermVectorOffsets;
            if (field.StoreTermVectorPayloads)
                flags |= LuceneFieldFlags.StoreTermVectorPayloads;
            if (field.StoreTermVectorPositions)
                flags |= LuceneFieldFlags.StoreTermVectorPositions;
            if (field.StoreTermVectors)
                flags |= LuceneFieldFlags.StoreTermVectors;
            return flags;
        }

        public static string GetBase64Flags(this FieldType field)
        {
            return Convert.ToBase64String(new[] { (byte)field.GetFlags() });
        }
    }

    [Flags]
    public enum LuceneFieldFlags : byte
    {
        None = 0,
        Indexed = 1 << 0,
        Stored = 1 << 1,
        Tokenized = 1 << 2,
        OmitNorms = 1 << 3,
        StoreTermVectorOffsets = 1 << 4,
        StoreTermVectorPayloads = 1 << 5,
        StoreTermVectorPositions = 1 << 6,
        StoreTermVectors = 1 << 7
    }

}