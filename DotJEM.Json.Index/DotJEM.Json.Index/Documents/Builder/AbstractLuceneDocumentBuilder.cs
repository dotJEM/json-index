using DotJEM.Json.Index.Diagnostics;
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
        void Add(IIndexableJsonField field);
    }

    public class LuceneDocument : ILuceneDocument
    {
        public Document Document { get; } = new Document();

        public void Add(IIndexableJsonField field)
        {
            foreach (IIndexableField x in field.LuceneFields)
                Document.Add(x);

            //TODO: (jmd 2020-08-10) Build Meta Data 
        }
    }

    public abstract class AbstractLuceneDocumentBuilder : JValueVisitor<IPathContext>, ILuceneDocumentBuilder
    {
        private readonly ILuceneJsonDocumentSerializer documentSerializer;
        private readonly ITypeBoundInfoStream infoStream;
        private readonly ILuceneDocument document = new LuceneDocument();

        public IInfoEventStream InfoStream => infoStream;

        protected AbstractLuceneDocumentBuilder(ILuceneJsonDocumentSerializer documentSerializer = null, IInfoEventStream infoStream = null)
        {
            this.infoStream = (infoStream ?? InfoEventStream.DefaultStream).Bind<AbstractLuceneDocumentBuilder>();
            this.documentSerializer = documentSerializer ?? new GZipLuceneJsonDocumentSerialier();
        }


        public ILuceneDocument Build(JObject json)
        {
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
