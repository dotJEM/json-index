using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
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

        //Task Merge(string contentType, JObject entity);
        Task Merge(string contentType, IFieldInformationCollection information);

        IReadOnlyFieldinformation Lookup(string fieldName);
        IReadOnlyFieldinformation Lookup(string contentType, string fieldName);
    }

    //TODO: How can we make the abstraction work with JSchema as well as our own simple collector?
    public interface IFieldInformationCollection : IEnumerable<IFieldInformation>
    {
        IFieldInformationCollection Merge(IFieldInformationCollection other);
        IReadOnlyFieldinformation Lookup(string fieldName);
    }

    public class FieldInformationCollector : IFieldInformationCollection
    {
        private IInfoEventStream InfoStream { get; }

        private readonly Dictionary<string, IFieldInformation> fields;
        public FieldInformationCollector(IInfoEventStream infoStream)
            : this(Enumerable.Empty<IFieldInformation>(), infoStream) { }

        public FieldInformationCollector(IEnumerable<IFieldInformation> fields, IInfoEventStream infoStream)
        {
            InfoStream = infoStream;
            this.fields = fields
                .GroupBy(info => info.Name)
                .Select(group => group.Aggregate((left, right) => left.Merge(right)))
                .ToDictionary(info => info.Name);
        }

        public void Add(string rootField, string fieldName, FieldType fieldType, JTokenType tokenType, Type type)
        {
            if (!fields.TryGetValue(fieldName, out IFieldInformation fieldInfo))
                fields.Add(fieldName, fieldInfo = new FieldInformation(fieldName));

            fieldInfo.Update(rootField, fieldType, tokenType, type);
        }
        public IReadOnlyFieldinformation Lookup(string fieldName)
        {
            return fields.TryGetValue(fieldName, out IFieldInformation field) ? field : null;
        }


        public IFieldInformationCollection Merge(IFieldInformationCollection other) => new FieldInformationCollector(this.Concat(other), InfoStream);

        public IEnumerator<IFieldInformation> GetEnumerator() => fields.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }

    public class FieldInformationCollection : IFieldInformationCollection
    {
        private readonly Dictionary<string, IFieldInformation> fields;

        public FieldInformationCollection() : this(Enumerable.Empty<IFieldInformation>()) { }

        public FieldInformationCollection(IEnumerable<IFieldInformation> fields)
        {
            this.fields = fields
                .GroupBy(info => info.Name)
                .Select(group => group.Aggregate((left, right) => left.Merge(right)))
                .ToDictionary(info => info.Name);
        }

        public IFieldInformationCollection Merge(IFieldInformationCollection other)
        {
            return new FieldInformationCollection(this.Concat(other));
        }
        public IReadOnlyFieldinformation Lookup(string fieldName)
        {
            return fields.TryGetValue(fieldName, out IFieldInformation field) ? field : null;
        }

        public IEnumerator<IFieldInformation> GetEnumerator() => fields.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IReadOnlyFieldinformation
    {
        IEnumerable<IFieldMetaData> MetaData { get; }
        string Name { get; }

    }
    public interface IFieldInformation : IReadOnlyFieldinformation
    {
        ITypeBoundInfoStream InfoStream { get; }

        IFieldInformation Update(string rootField, FieldType fieldType, JTokenType tokenType, Type type);

        IFieldInformation Merge(IFieldInformation other);
    }

    public class FieldInformation : IFieldInformation
    {
        public string Name { get; }
        public ITypeBoundInfoStream InfoStream { get; }

        private readonly ConcurrentDictionary<string, IFieldMetaData> infos;

        public IEnumerable<IFieldMetaData> MetaData => infos.Values;

        public FieldInformation(string name, IInfoEventStream infoStream = null) 
            : this(name, Enumerable.Empty<IFieldMetaData>(), infoStream) {}

        private FieldInformation(string name, IEnumerable<IFieldMetaData> metaData, IInfoEventStream infoStream = null)
        {
            InfoStream = (infoStream ?? InfoEventStream.DefaultStream).Bind<FieldInformation>();
            Name = name;
            infos = new ConcurrentDictionary<string, IFieldMetaData>(
                metaData.ToDictionary(meta => meta.Key)
                );
        }

        public IFieldInformation Update(string rootField, FieldType fieldType, JTokenType tokenType, Type type)
        {
            // Consider immuteable.
            FieldMetaData meta = new FieldMetaData(rootField, fieldType, tokenType, type);
            infos.TryAdd(meta.Key, meta);
            InfoStream.Debug($"Adding field meta data ({meta.Key}) to field {Name}", new []{ meta });
            return this;
        }

        public IFieldInformation Merge(IFieldInformation other)
        {
            return new FieldInformation(Name, other.MetaData.Concat(MetaData).Distinct(new FieldMetaDataComparer()), InfoStream);
        }

        private class FieldMetaDataComparer : IEqualityComparer<IFieldMetaData>
        {
            public bool Equals(IFieldMetaData x, IFieldMetaData y) => StringComparer.Ordinal.Equals(x.Key, y.Key);
            public int GetHashCode(IFieldMetaData obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
        }
    }

    public class DefaultFieldInformationManager : IFieldInformationManager
    {
        public IInfoEventStream InfoStream { get; }

        private readonly IFieldInformationCollection allFields = new FieldInformationCollection();
        private readonly ConcurrentDictionary<string, IFieldInformationCollection> contentTypes = new ConcurrentDictionary<string, IFieldInformationCollection>();

        public IFieldResolver Resolver { get; }

        public DefaultFieldInformationManager(IFieldResolver resolver, IInfoEventStream infoStream = null)
        {
            Resolver = resolver;
            InfoStream = infoStream ?? InfoEventStream.DefaultStream.Bind<DefaultFieldInformationManager>();
        }

        public IEnumerable<string> ContentTypes => contentTypes.Keys;
        public IEnumerable<string> AllFields => allFields.Select(info => info.Name);

        public async Task Merge(string contentType, IFieldInformationCollection information)
        {
            await Task.Run(() =>
            {
                contentTypes.AddOrUpdate(contentType,
                    key => new FieldInformationCollection(information),
                    (key, collection) => collection.Merge(information));
                allFields.Merge(information);
            });
        }

        public IReadOnlyFieldinformation Lookup(string fieldName)
        {
            return allFields.Lookup(fieldName);
        }

        public IReadOnlyFieldinformation Lookup(string contentType, string fieldName)
        {
            if (contentTypes.TryGetValue(contentType, out IFieldInformationCollection fields))
                return fields.Lookup(fieldName);
            return null;
        }
    }


}