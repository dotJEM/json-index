﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
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

        IFieldInfoCollection FieldInfo { get; }
    }

    public abstract class AbstractLuceneDocumentBuilder : JValueVisitor<IJsonPathContext>, ILuceneDocumentBuilder
    {
        private readonly IFieldResolver fields;
        private readonly ILuceneJsonDocumentSerializer documentSerializer;
        private readonly ITypeBoundInfoStream infoStream;

        public IInfoEventStream InfoStream => infoStream;

        protected AbstractLuceneDocumentBuilder(IFieldResolver fields = null, ILuceneJsonDocumentSerializer documentSerializer = null, IInfoEventStream infoStream = null)
        {
            this.infoStream = (infoStream ?? InfoEventStream.DefaultStream).Bind<AbstractLuceneDocumentBuilder>();

            this.fields = fields ?? new FieldResolver();
            this.documentSerializer = documentSerializer ?? new GZipLuceneJsonDocumentSerialier();
        }

        public Document Document { get; private set; }

        public Document Build(JObject json)
        {
            Document = new Document();
            infoCollector = new FieldInfoCollectionBuilder(InfoStream);

            JsonPathContext context = new JsonPathContext(this);
            documentSerializer.SerializeTo(json, Document);

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

            public void Apply(IFieldStrategy strategy)
            {
                Type strategyType = strategy.GetType();
                JTokenType jsonType = FieldContext.Value.Type;
                JsonFieldInfoBuilder info = new JsonFieldInfoBuilder(FieldContext.Field);
                foreach (IJsonIndexableField field in strategy.CreateFields(FieldContext))
                {
                    builder.Add(field.Field);
                    info.Add(field.FieldName, jsonType, field.ClrType, field.LuceneType, strategyType, field.MetaData);
                }
                builder.AddFieldInfo(info.Build());
            }
        }


        private FieldInfoCollectionBuilder infoCollector;
        public IFieldInfoCollection FieldInfo => infoCollector.Build();

        private void AddFieldInfo(IJsonFieldInfo fieldInfo)
        {
            infoCollector.Add(fieldInfo);
        }
    }
}
