using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.Serialization;
using DotJEM.Json.Visitor;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface ILuceneDocumentBuilder
    {
        IInfoEventStream InfoStream { get; }
        Document Document { get; }

        Document Build(JObject json);

        IFieldInformationCollection FieldInfo { get; }
    }

    public abstract class AbstractLuceneDocumentBuilder : JValueVisitor<IJsonPathContext>, ILuceneDocumentBuilder
    {
        private readonly IJsonSerializer serializer;
        private FieldInformationCollector infoCollector;
        public IInfoEventStream InfoStream { get; }

        protected AbstractLuceneDocumentBuilder(IJsonSerializer serializer = null, IInfoEventStream infoStream = null)
        {
            InfoStream = infoStream ?? InfoEventStream.DefaultStream;
            this.serializer = serializer ?? new GZipJsonSerialier();
        }

        public Document Document { get; private set; }

        public IFieldInformationCollection FieldInfo => infoCollector;

        public Document Build(JObject json)
        {
            Document = new Document();
            infoCollector = new FieldInformationCollector(InfoStream);

            JsonPathContext context = new JsonPathContext(this);

            //TODO: Perhaps we should just pass the document?
            Add(new StoredField("$$RAW", serializer.Serialize(json)));

            Visit(json, context);
            return Document;
        }

        protected override void Visit(JArray json, IJsonPathContext context)
        {
            int num = 0;
            foreach (JToken self in json)
                self.Accept(this, context.Next(num++, self));
        }

        protected override void Visit(JProperty json, IJsonPathContext context)
            => json.Value.Accept(this, context.Next(json.Name, json.Value));

        private void Add(IIndexableField field)
            => Document.Add(field);

        private void Info(string rootField, string fieldName, FieldType fieldType, JTokenType tokenType, Type type)
            => infoCollector.Add(rootField, fieldName, fieldType, tokenType, type);

        /*
         * TODO: Because we are adding configurabel strategies, much of the pieces below should be replaced by
         * a more simple concept of IFieldContext...
         *
         * A Field context will capture the current path and value... (Just like the IJsonPathContext)
         * however it would be far more simple in that it is not meant for navigation like the PathContext is.
         *
         * Instead it's merely meant for input to a FieldFactory, which replaces the FieldBuilder (It's a factory as things are now anyways).
         */

        public class JsonPathContext : IJsonPathContext
        {
            private readonly AbstractLuceneDocumentBuilder builder;

            public string Path { get; }
            public JToken Value { get; }
            public IFieldContext FieldContext { get; }

            public JsonPathContext(AbstractLuceneDocumentBuilder builder, string path = "", JToken value = null)
            {
                Path = path;
                this.builder = builder;
                this.Value = value;
                this.FieldContext = new FieldContext(path, value);
            }

            public IJsonPathContext Next(int index) => throw new NotSupportedException("Use the overloaded method Next(int, JToken) instead.");
            public IJsonPathContext Next(int index, JToken value) => new JsonPathContext(builder, Path, value);
            public IJsonPathContext Next(string name) => throw new NotSupportedException("Use the overloaded method Next(string, JToken) instead.");
            public IJsonPathContext Next(string name, JToken value) => new JsonPathContext(builder, Path == "" ? name : Path + "." + name, value);

            public void Apply<T>() where T : IFieldStrategy, new()
                => Apply(new T());

            public void Apply(IFieldStrategy strategy)
            {
                //return new StringField(fullpath, transform(self.DeserializedValue), Field.Store.NO);
                //builder.Add(new StringField(fullpath, transform(deserializedValue), Field.Store.NO));
                //builder.Info(path, fullpath, StringField.TYPE_NOT_STORED, tokenType, typeof(string));
                
                string sourceFieldName = FieldContext.Field;
                Type strategyType = strategy.GetType();
                JTokenType jsonType = FieldContext.Value.Type;
                
                foreach (IJsonIndexableField field in strategy.CreateFields(FieldContext))
                {
                    builder.Add(field.Field);


                    //TODO: This needs fixing.
                    string fieldName = field.FieldName;
                    Type clrType = field.ClrType;
                    FieldType luceneType = field.LuceneType;
                    FieldData[] data = field.Data;

                    builder.Info(sourceFieldName, fieldName, luceneType, jsonType, clrType);

                }
            }
        }
    }
}
