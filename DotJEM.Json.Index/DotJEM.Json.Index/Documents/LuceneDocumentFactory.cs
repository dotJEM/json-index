using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Documents.Builder;
using DotJEM.Json.Index.Documents.Info;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents
{
    public interface ILuceneDocumentFactory
    {
        Task<LuceneDocumentEntry> Create(JObject entity);
        IEnumerable<LuceneDocumentEntry> Create(IEnumerable<JObject> entity);
    }

    public class LuceneDocumentFactory : ILuceneDocumentFactory
    {
        private readonly IFieldInformationManager fieldsInfo;
        private readonly IFactory<ILuceneDocumentBuilder> builderFactory;

        public LuceneDocumentFactory(IFieldInformationManager fieldsInformationManager)
            : this(fieldsInformationManager, new FuncFactory<ILuceneDocumentBuilder>(() => new LuceneDocumentBuilder()))
        {
        }

        public LuceneDocumentFactory(IFieldInformationManager fieldsInformationManager, IFactory<ILuceneDocumentBuilder> builderFactory)
        {
            this.fieldsInfo = fieldsInformationManager ?? throw new ArgumentNullException(nameof(fieldsInformationManager));
            this.builderFactory = builderFactory ?? throw new ArgumentNullException(nameof(builderFactory));
        }

        public async Task<LuceneDocumentEntry> Create(JObject entity)
        {
            return await Task.Run(() =>
            {
                ILuceneDocumentBuilder builder = builderFactory.Create();
                string contentType = fieldsInfo.Resolver.ContentType(entity);

                Document doc = builder.Build(entity);
                fieldsInfo.Merge(contentType, builder.FieldInfo);

                return new LuceneDocumentEntry(fieldsInfo.Resolver.Identity(entity), contentType, doc);
            }).ConfigureAwait(false);
        }

        public IEnumerable<LuceneDocumentEntry> Create(IEnumerable<JObject> docs)
        {
            AsyncList<LuceneDocumentEntry> collection = new AsyncList<LuceneDocumentEntry>();
            #pragma warning disable 4014
            FillAsync(docs, collection);
            #pragma warning restore 4014
            return collection;
        }

        private async Task FillAsync(IEnumerable<JObject> docs, AsyncList<LuceneDocumentEntry> collection)
        {
            await Task.WhenAll(docs.Select(async (json, index) =>
            {
                try
                {
                    collection.Add(await Create(json));
                }
                catch (Exception e)
                {
                    collection.ReportFailure(e, json);
                }
            }));
            collection.CompleteAdding();
        }

        private class AsyncList<T> : IEnumerable<T> where T : class
        {
            private event EventHandler<EventArgs> Changed;

            private bool completed = false;
            private readonly object padlock = new object();
            private readonly List<T> items = new List<T>();
            //private readonly List<(Exception, JObject)> failures = new List<(Exception, JObject)>();

            private int Count => items.Count;

            public void Add(T item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (completed)
                    throw new InvalidOperationException("Cannot add more items to a AsyncList that is marked as completed.");

                lock (padlock) items.Add(item);
                RaiseChanged();
            }

            public void CompleteAdding()
            {
                completed = true;
                RaiseChanged();
            }

            public IEnumerator<T> GetEnumerator() => new DocumentsListEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void ReportFailure(Exception exception, JObject json)
            {
                //TODO: We need a way to report errors up stream... (Exception or similar).
                //failures.Add((exception, json));
            }

            private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

            private class DocumentsListEnumerator : IEnumerator<T>
            {
                private readonly AsyncList<T> source;
                private readonly object padlock = new object();
                private int index = -1;

                public DocumentsListEnumerator(AsyncList<T> source)
                {
                    this.source = source;
                    this.source.Changed += SourceChanged;
                }

                private void SourceChanged(object sender, EventArgs eventArgs)
                {
                    lock (padlock) Monitor.PulseAll(padlock);
                }

                public bool MoveNext()
                {
                    lock (padlock)
                    {
                        index++;
                        while (true)
                        {
                            if (index < source.Count)
                                return true;

                            if (source.completed)
                                return false;

                            Monitor.Wait(padlock);
                        }
                    }
                }


                public void Reset()
                {
                    lock (padlock)
                    {
                        index = -1;
                        Monitor.PulseAll(padlock);
                    }
                }

                public T Current => source.items[index];
                object IEnumerator.Current => Current;
                public void Dispose() => this.source.Changed -= SourceChanged;
            }
        }
    }
}
