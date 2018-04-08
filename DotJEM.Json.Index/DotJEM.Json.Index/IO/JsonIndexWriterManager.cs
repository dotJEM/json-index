using System;
using System.Collections.Generic;
using System.Text;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.IO
{
    public interface IIndexWriterManager : IDisposable
    {
        IndexWriter Writer { get; }
    }

    public class IndexWriterManager : Disposable, IIndexWriterManager
    {
        public IndexWriter Writer { get; }

        public IndexWriterManager(IndexWriter writer)
        {
            Writer = writer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Writer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
