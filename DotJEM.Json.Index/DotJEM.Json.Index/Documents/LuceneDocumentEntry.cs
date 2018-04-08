using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Documents
{
    public class LuceneDocumentEntry
    {
        public Term Key { get; }
        public string ContentType { get; }
        public Document Document { get; }

        public LuceneDocumentEntry(Term key, string contentType, Document document)
        {
            Key = key;
            ContentType = contentType;
            Document = document;
        }

        public void Deconstruct(out Term key, out string contentType, out Document document)
        {
            key = Key;
            contentType = ContentType;
            document = Document;
        }

        public void Deconstruct(out Term key, out Document document)
        {
            key = Key;
            document = Document;
        }
    }
}
