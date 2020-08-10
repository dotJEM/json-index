using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        IEnumerable<IJsonFieldInfo> AllFields { get; }

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

    public interface IJsonFieldInfo : IEnumerable<ILuceneFieldInfo>
    {
        string Name { get; }
        JTokenType TokenType { get; }
        IEnumerable<Type> ClrType { get; }
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

    
}