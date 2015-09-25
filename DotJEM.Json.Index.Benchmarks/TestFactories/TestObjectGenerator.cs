using System.Collections;
using System.Collections.Generic;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Benchmarks.TestFactories
{
    public class TestObjectGenerator : IEnumerable<Document>
    {
        private bool stop = false;
        private readonly int limit;
        private readonly HashSet<string> contentTypes = new HashSet<string>(new[] { "order", "person", "product", "account", "storage", "address", "payment", "delivery", "token", "shipment" });
        private readonly RandomTextGenerator textGenerator;

        public TestObjectGenerator(int limit) : this(limit, new RandomTextGenerator())
        {
        }

        public TestObjectGenerator(int limit, RandomTextGenerator textGenerator)
        {
            this.limit = limit;
            this.textGenerator = textGenerator;
        }

        public IEnumerator<Document> GetEnumerator()
        {
            int count = 0;
            while (!stop && count++ < limit)
            {
                string contentType = RandomContentType();
                yield return new Document(contentType, RandomDocument(contentType));
            }
        }

        private string RandomContentType()
        {
            return contentTypes.RandomItem();
        }

        private JObject RandomDocument(string contentType)
        {
            string text = textGenerator.RandomText();
            //TODO: Bigger document and use contentype for propper stuff.
            return JObject.FromObject(new
            {
                source = text,
                content = textGenerator.Paragraph(text),
                keys = textGenerator.Words(text, 4, 5)
            });
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Stop()
        {
            stop = true;
        }
    }
}
