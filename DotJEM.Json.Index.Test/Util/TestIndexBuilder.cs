using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Test.Util
{
    public static class GuidExt
    {
        public static Guid ToGuid(this long value)
        {
            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(value), guidData, 8);
            return new Guid(guidData);
        }

        public static long ToLong(this Guid guid)
        {
            if (BitConverter.ToInt64(guid.ToByteArray(), 8) != 0)
                throw new OverflowException("Value was either too large or too small for an Int64.");
            return BitConverter.ToInt64(guid.ToByteArray(), 0);
        }
    }

    public class TestIndexBuilder
    {
        private readonly IStorageIndex index;
        private long count;

        private readonly List<JObject> buffer = new List<JObject>();   

        public TestIndexBuilder() : this(new LuceneStorageIndex(new LuceneMemmoryIndexStorage(new WhitespaceAnalyzer(LuceneVersion.LUCENE_48))))
        {
        }

        public TestIndexBuilder(IStorageIndex index)
        {
            this.index = index;
            index.Configuration.SetTypeResolver("contentType").ForAll().SetIdentity("id");

        }

        public TestIndexBuilder Document(Func<TestDocumentBuilder, TestDocumentBuilder> build)
        {
            return Document("Document", build);
        }

        public TestIndexBuilder Document(string contentType, Func<TestDocumentBuilder, TestDocumentBuilder> build)
        {
            buffer.Add(build(new TestDocumentBuilder(contentType, (count++).ToGuid())).Build());
            if (buffer.Count > 20000)
                Flush();
            return this;
        }

        public TestIndexBuilder Document(Document prototype, Func<TestDocumentBuilder, TestDocumentBuilder> build)
        {
            buffer.Add(build(new TestDocumentBuilder(prototype, (count++).ToGuid())).Build());
            if (buffer.Count > 20000)
                Flush();
            return this;
        }

        private void Flush()
        {
            index.WriteAll(buffer);
            buffer.Clear();
        }

        public TestIndexBuilder Insert(TestDocumentBuilder testDocumentBuilder, JObject json)
        {
            return this;
        }

        public IStorageIndex Build()
        {
            Flush();
            return index.Commit();
        }
    }

    public class Document
    {
        public Guid Id { get; set; }
        public JObject Data { get; private set; }
        public string ContentType { get; private set; }

        public Document(string contentType, JObject data)
        {
            Data = data;
            ContentType = contentType;
        }
    }
}