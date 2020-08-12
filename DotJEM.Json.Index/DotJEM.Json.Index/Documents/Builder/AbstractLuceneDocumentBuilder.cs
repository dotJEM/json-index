using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Serialization;
using DotJEM.Json.Visitor;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface ILuceneDocumentBuilder
    {
        IInfoEventStream InfoStream { get; }
        ILuceneDocument Build(JObject json);
    }

    public interface ILuceneDocument
    {
        Document Document { get; } 
        IContentTypeInfo Info { get; } 
        void Add(IIndexableJsonField field);
    }

    public class LuceneDocument : ILuceneDocument
    {
        public Document Document { get; } = new Document();
        
        public IContentTypeInfo Info { get; }

        public LuceneDocument(string contentType)
        {
            Info = new ContentTypeInfo(contentType);
        }

        public void Add(IIndexableJsonField field)
        {
            foreach (IIndexableField x in field.LuceneFields)
                Document.Add(x);

            Info.Add(field.Info());
        }
    }

    public abstract class AbstractLuceneDocumentBuilder : JValueVisitor<IPathContext>, ILuceneDocumentBuilder
    {
        private readonly IFieldResolver resolver;
        private readonly ILuceneJsonDocumentSerializer documentSerializer;
        private readonly ITypeBoundInfoStream infoStream;

        private ILuceneDocument document;

        public IInfoEventStream InfoStream => infoStream;

        protected AbstractLuceneDocumentBuilder(IFieldResolver resolver = null, ILuceneJsonDocumentSerializer documentSerializer = null, IInfoEventStream infoStream = null)
        {
            this.resolver = resolver ?? new FieldResolver();
            this.infoStream = (infoStream ?? InfoEventStream.DefaultStream).Bind<AbstractLuceneDocumentBuilder>();
            this.documentSerializer = documentSerializer ?? new GZipLuceneJsonDocumentSerialier();
        }


        public ILuceneDocument Build(JObject json)
        {
            document = new LuceneDocument(resolver.ContentType(json));
            PathContext context = new PathContext(this);
            documentSerializer.SerializeTo(json, document.Document);
            Visit(json, context);
            return document;
        }

        protected override void Visit(JArray json, IPathContext context)
        {
            int num = 0;
            foreach (JToken self in json)
                self.Accept(this, context.Next(num++));
        }

        protected override void Visit(JProperty json, IPathContext context)
            => json.Value.Accept(this, context.Next(json.Name));
        
        protected void Add(IIndexableJsonField field) 
            => document.Add(field);

        protected void Add(IEnumerable<IIndexableJsonField> fields)
        {
            foreach (IIndexableJsonField field in fields)
            {
                Add(field);
            }
        }
        /*
         * TODO: Because we are adding configurabel strategies, much of the pieces below should be replaced by
         * a more simple concept of IFieldContext...
         *
         * A Field context will capture the current path and value... (Just like the IJsonPathContext)
         * however it would be far more simple in that it is not meant for navigation like the PathContext is.
         *
         * Instead it's merely meant for input to a FieldFactory, which replaces the FieldBuilder (It's a factory as things are now anyways).
         */

        public class PathContext : IPathContext
        {
            private readonly AbstractLuceneDocumentBuilder builder;

            public string Path { get; }

            public PathContext(AbstractLuceneDocumentBuilder builder, string path = "")
            {
                Path = path;
                this.builder = builder;
            }

            public IPathContext Next(int index)  => new PathContext(builder, Path);
            public IPathContext Next(string name) => new PathContext(builder, Path == "" ? name : Path + "." + name);
        }
    }
}
