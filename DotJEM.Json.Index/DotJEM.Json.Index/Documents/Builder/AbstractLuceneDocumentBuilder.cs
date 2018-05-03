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
            public IFieldContext FieldContext { get; set; }

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

            public IFieldBuilder<TValue> FieldBuilder<TValue>() => new LuceneFieldBuilder<TValue>(Path, Value, builder, token => token.ToObject<TValue>());
            public IFieldBuilder<TValue> FieldBuilder<TValue>(Func<JToken, TValue> converter) => new LuceneFieldBuilder<TValue>(Path, Value, builder, converter);
        }


        public class LuceneFieldBuilder<TValue> : IFieldBuilder<TValue>
        {
            private readonly string path;
            private readonly JTokenType tokenType;
            private readonly TValue deserializedValue;
            private readonly AbstractLuceneDocumentBuilder builder;

            public LuceneFieldBuilder(string path, JToken value, AbstractLuceneDocumentBuilder builder, Func<JToken, TValue> converter)
            {
                this.path = path;
                this.builder = builder;
                this.tokenType = value.Type;
                this.deserializedValue = converter(value);
            }

            public IFieldBuilder<TValue> AddStringField()
                => InternalAddStringField(path, v => v.ToString());

            public IFieldBuilder<TValue> AddStringField(Func<TValue, string> transform)
                => InternalAddStringField(path, transform);

            public IFieldBuilder<TValue> AddStringField(string extension, Func<TValue, string> transform)
                => InternalAddStringField($"{path}.{extension}", transform);

            public IFieldBuilder<TValue> InternalAddStringField(string fullpath, Func<TValue, string> transform)
            {
                builder.Add(new StringField(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, StringField.TYPE_NOT_STORED, tokenType, typeof(string));
                return this;
            }

            public IFieldBuilder<TValue> AddTextField()
                => InternalAddTextField(path, v => v.ToString());
            public IFieldBuilder<TValue> AddTextField(Func<TValue, string> transform)
                => InternalAddTextField(path, transform);

            public IFieldBuilder<TValue> AddTextField(string extension, Func<TValue, string> transform)
                => InternalAddTextField($"{path}.{extension}", transform);

            public IFieldBuilder<TValue> InternalAddTextField(string fullpath, Func<TValue, string> transform)
            {
                builder.Add(new TextField(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, TextField.TYPE_NOT_STORED, tokenType, typeof(string));
                return this;
            }

            public IFieldBuilder<TValue> AddInt32Field()
                => InternalAddInt32Field(path, v => Convert.ToInt32(v));
            public IFieldBuilder<TValue> AddInt32Field(Func<TValue, int> transform)
                => InternalAddInt32Field(path, transform);

            public IFieldBuilder<TValue> AddInt32Field(string extension, Func<TValue, int> transform)
                => InternalAddInt32Field($"{path}.{extension}", transform);

            private IFieldBuilder<TValue> InternalAddInt32Field(string fullpath, Func<TValue, int> transform)
            {
                builder.Add(new Int32Field(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, Int32Field.TYPE_NOT_STORED, tokenType, typeof(long));
                return this;
            }

            public IFieldBuilder<TValue> AddInt64Field()
                => InternalAddInt64Field(path, v => Convert.ToInt64(v));
            public IFieldBuilder<TValue> AddInt64Field(Func<TValue, long> transform)
                => InternalAddInt64Field(path, transform);

            public IFieldBuilder<TValue> AddInt64Field(string extension, Func<TValue, long> transform)
                => InternalAddInt64Field($"{path}.{extension}", transform);

            private IFieldBuilder<TValue> InternalAddInt64Field(string fullpath, Func<TValue, long> transform)
            {
                builder.Add(new Int64Field(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, Int64Field.TYPE_NOT_STORED, tokenType, typeof(long));
                return this;
            }

            public IFieldBuilder<TValue> AddSingleField()
                => InternalAddSingleField(path, v => Convert.ToSingle(v));
            public IFieldBuilder<TValue> AddSingleField(Func<TValue, float> transform)
                => InternalAddSingleField(path, transform);

            public IFieldBuilder<TValue> AddSingleField(string extension, Func<TValue, float> transform)
                => InternalAddSingleField($"{path}.{extension}", transform);

            private IFieldBuilder<TValue> InternalAddSingleField(string fullpath, Func<TValue, float> transform)
            {
                builder.Add(new SingleField(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, SingleField.TYPE_NOT_STORED, tokenType, typeof(long));
                return this;
            }

            public IFieldBuilder<TValue> AddDoubleField()
                => InternalAddDoubleField(path, v => Convert.ToDouble(v));
            public IFieldBuilder<TValue> AddDoubleField(Func<TValue, double> transform)
                => InternalAddDoubleField(path, transform);

            public IFieldBuilder<TValue> AddDoubleField(string extension, Func<TValue, double> transform)
                => InternalAddDoubleField($"{path}.{extension}", transform);

            private IFieldBuilder<TValue> InternalAddDoubleField(string fullpath, Func<TValue, double> transform)
            {
                builder.Add(new DoubleField(fullpath, transform(deserializedValue), Field.Store.NO));
                builder.Info(path, fullpath, DoubleField.TYPE_NOT_STORED, tokenType, typeof(long));
                return this;
            }
        }
    }
}
