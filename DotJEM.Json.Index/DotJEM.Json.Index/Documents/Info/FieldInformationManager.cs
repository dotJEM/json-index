using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Builder;
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
        IEnumerable<IIndexableJsonFieldInfo> AllFields { get; }
        IEnumerable<IIndexableFieldInfo> AllIndexedFields { get; }

        void Merge(string contentType, IContentTypeInfo info);

        IIndexableJsonFieldInfo Lookup(string fieldName);
        IIndexableJsonFieldInfo Lookup(string contentType, string fieldName);
    }

    public class DefaultFieldInformationManager : IFieldInformationManager
    {
        private Dictionary<string, IContentTypeInfo> contentTypes = new Dictionary<string, IContentTypeInfo>();
        private ConcurrentDictionary<string, IContentTypeInfo> map = new ConcurrentDictionary<string, IContentTypeInfo>();

        public IInfoEventStream InfoStream { get; }
        public IFieldResolver Resolver { get; }
        public IEnumerable<string> ContentTypes => contentTypes.Keys;

        public IEnumerable<IIndexableJsonFieldInfo> AllFields { get; }
        public IEnumerable<IIndexableFieldInfo> AllIndexedFields { get; }

        public DefaultFieldInformationManager(IFieldResolver resolver, IInfoEventStream infoStream = null)
        {
            InfoStream = infoStream ?? InfoEventStream.DefaultStream.Bind<DefaultFieldInformationManager>();
            Resolver = resolver;
        }

        public void Merge(string contentType, IContentTypeInfo info)
        {
            map.AddOrUpdate(contentType, info, (_, current) => current.Merge(info));
            contentTypes[contentType] = info;
        }

        public IIndexableJsonFieldInfo Lookup(string fieldName)
        {
            return null;
        }

        public IIndexableJsonFieldInfo Lookup(string contentType, string fieldName)
        {
            return null;
        }
    }

    public static class IContentTypeInfoExt
    {
        public static IContentTypeInfo Merge(this IContentTypeInfo self, IContentTypeInfo other)
        {
            return other.FieldInfos.Aggregate(self, (info, fieldInfo) => info.Add(fieldInfo));
        }
    }

    //public interface IJsonFieldInfo
    //{
    //    string Name { get; }
    //    JTokenType TokenType { get; }
    //    IEnumerable<Type> SourceTypes { get; }
    //}
    
    //public interface ILuceneFieldInfo
    //{
    //    string LuceneField { get; }
    //    FieldType LuceneType { get; }
    //    Type StrategyType { get; }
    //    JObject MetaData { get; }
    //}

    //public class LuceneFieldInfo : ILuceneFieldInfo
    //{
    //    public string Key { get; }
    //    public string LuceneField { get; }
    //    public FieldType LuceneType { get; }
    //    public Type StrategyType { get; }
    //    public JObject MetaData { get; }

    //    public LuceneFieldInfo(string luceneField, JTokenType jsonType, Type clrType, FieldType luceneType, Type strategyType, JObject metaData)
    //    {
    //        LuceneField = luceneField;
    //        LuceneType = luceneType;
    //        StrategyType = strategyType;
    //        MetaData = metaData;
    //        Key =
    //            $"{luceneField};{(int)luceneType.DocValueType};{(int)luceneType.IndexOptions};{(int)luceneType.IndexOptions}" +
    //            $";{(int)luceneType.DocValueType};" +
    //            $"{luceneType.GetBase64Flags()};{luceneType.NumericPrecisionStep};{(int)jsonType};{clrType};{strategyType}";
    //    }
    //}

    
}