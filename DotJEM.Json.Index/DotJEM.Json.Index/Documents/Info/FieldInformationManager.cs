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
        IEnumerable<string> AllFields { get; }

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

    public class FieldInfoCollectionBuilder
    {
        private readonly ITypeBoundInfoStream infoStream;
        public IInfoEventStream InfoStream => infoStream;

        private readonly Dictionary<string, IJsonFieldInfo> fields = new Dictionary<string, IJsonFieldInfo>();

        public FieldInfoCollectionBuilder(IInfoEventStream infoStream)
        {
            this.infoStream = infoStream.Bind<FieldInfoCollectionBuilder>();
        }

        public void Add(IJsonFieldInfo info)
        {
            if (fields.TryGetValue(info.Name, out IJsonFieldInfo fieldInfo))
            {
                infoStream.Debug($"Updating field information {info.Name}", new []{ info });
                fields[info.Name] = fieldInfo.Merge(info);
            }
            else
            {
                infoStream.Debug($"Adding field information {info.Name}", new []{ info });
                fields[info.Name] = info;
            }
        }

        public IFieldInfoCollection Build()
        {
            return new FieldInfoCollection(fields);
        }
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
    
    public class DefaultFieldInformationManager : IFieldInformationManager
    {
        public IInfoEventStream InfoStream { get; }

        private IFieldInfoCollection allFields = new FieldInfoCollection();
        private readonly ConcurrentDictionary<string, IFieldInfoCollection> contentTypes = new ConcurrentDictionary<string, IFieldInfoCollection>();

        public IFieldResolver Resolver { get; }

        public DefaultFieldInformationManager(IFieldResolver resolver, IInfoEventStream infoStream = null)
        {
            Resolver = resolver;
            InfoStream = infoStream ?? InfoEventStream.DefaultStream.Bind<DefaultFieldInformationManager>();
        }

        public IEnumerable<string> ContentTypes => contentTypes.Keys;
        public IEnumerable<string> AllFields => allFields.Select(info => info.Name);

        public void Merge(string contentType, IFieldInfoCollection info)
        {
            contentTypes.AddOrUpdate(contentType,
                key => new FieldInfoCollection(info),
                (key, collection) => collection.Merge(info));
            //TODO: Find another way that won't lock the resource. Either some queing or branching...
            //      Branching is ok when we merge back in because we don't care if we are presented with the same
            //      information multiple times.
            lock (allFields)
            {
                this.allFields = allFields.Merge(info);
            }
        }

        public IJsonFieldInfo Lookup(string fieldName)
        {
            return allFields.Lookup(fieldName);
        }

        public IJsonFieldInfo Lookup(string contentType, string fieldName)
        {
            if (contentTypes.TryGetValue(contentType, out IFieldInfoCollection fields))
                return fields.Lookup(fieldName);
            return null;
        }
    }

    public interface IJsonFieldInfo : IEnumerable<ILuceneFieldInfo>
    {
        string Name { get; }
        IJsonFieldInfo Merge(IJsonFieldInfo other);
    }

    public class JsonFieldInfo : IJsonFieldInfo
    {
        private readonly IDictionary<string, ILuceneFieldInfo> mappedToFields;

        public string Name { get; }

        public JsonFieldInfo(string name, IDictionary<string, ILuceneFieldInfo> mappedToFields)
        {
            Name = name;
            if (!(mappedToFields is Dictionary<string, ILuceneFieldInfo> fieldsDictionary))
                fieldsDictionary = new Dictionary<string, ILuceneFieldInfo>(mappedToFields);
            this.mappedToFields = fieldsDictionary;
        }

        public IJsonFieldInfo Merge(IJsonFieldInfo other)
        {
            JsonFieldInfoBuilder builder = new JsonFieldInfoBuilder(Name);
            this.Aggregate(builder, (b, info) => b.Add(info));
            other.Aggregate(builder, (b, info) => b.Add(info));
            return builder.Build();
        }

        public IEnumerator<ILuceneFieldInfo> GetEnumerator() => mappedToFields.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class JsonFieldInfoBuilder
    {
        private readonly string name;
        private readonly Dictionary<string, ILuceneFieldInfo> infos = new Dictionary<string, ILuceneFieldInfo>();

        public JsonFieldInfoBuilder(string name)
        {
            this.name = name;
        }

        public JsonFieldInfoBuilder Add(ILuceneFieldInfo info)
        {
            infos[info.Key] = infos.TryGetValue(info.Key, out ILuceneFieldInfo fieldInfo)
                ? fieldInfo.Merge(info)
                : info;
            return this;
        }

        public JsonFieldInfoBuilder Add(string luceneField, JTokenType jsonType, Type clrType, FieldType luceneType, Type strategyType, FieldMetaData[] metaData)
            => Add(new LuceneFieldInfo(luceneField, jsonType, clrType, luceneType, strategyType, metaData));

        public IJsonFieldInfo Build()
        {
            return new JsonFieldInfo(name, infos);
        }
    }
    public static class LuceneFieldInfoExtentions
    {
        public static ILuceneFieldInfo Merge(this ILuceneFieldInfo self, ILuceneFieldInfo other)
        {
            //TODO: Merge MetaData, the rest is the same as pr. the key.
            //      Ideally MetaData should also give input to the key and we should hash it... But for now SIMPLE STUFF.
            return other;
        }
    }
    public interface ILuceneFieldInfo
    {
        string Key { get; }
        string LuceneField { get; }
        JTokenType JsonType { get; }
        Type ClrType { get; }
        FieldType LuceneType { get; }
        Type StrategyType { get; }
        FieldMetaData[] MetaData { get; }
    }

    public class LuceneFieldInfo : ILuceneFieldInfo
    {
        public string Key { get; }
        public string LuceneField { get; }
        public JTokenType JsonType { get; }
        public Type ClrType { get; }
        public FieldType LuceneType { get; }
        public Type StrategyType { get; }
        public FieldMetaData[] MetaData { get; }

        public LuceneFieldInfo(string luceneField, JTokenType jsonType, Type clrType, FieldType luceneType, Type strategyType, FieldMetaData[] metaData)
        {
            LuceneField = luceneField;
            JsonType = jsonType;
            ClrType = clrType;
            LuceneType = luceneType;
            StrategyType = strategyType;
            MetaData = metaData;
            Key =
                $"{luceneField};{(int)luceneType.DocValueType};{(int)luceneType.IndexOptions};{(int)luceneType.IndexOptions}" +
                $";{(int)luceneType.DocValueType};" +
                $"{luceneType.GetBase64Flags()};{luceneType.NumericPrecisionStep};{(int)jsonType};{clrType};{strategyType}";

        }
    }

    public interface IFieldMetaData
    {
        string Name { get; }
        string Value { get; }
    }

    public struct FieldMetaData : IFieldMetaData
    {
        public string Name { get; }
        public string Value { get; }

        public FieldMetaData(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}