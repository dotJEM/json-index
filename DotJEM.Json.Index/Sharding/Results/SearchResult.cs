using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Results
{
    public interface IJsonSearchResult : IEnumerable<IJsonHit>
    {
        long TotalCount { get; }
        IEnumerable<dynamic> Documents { get; }
    }

    public class JsonSearchResult : IJsonSearchResult
    {
        private readonly IJsonIndexSearcher searcher;
        private readonly Lazy<IEnumerable<IJsonHit>> results;
         
        private int skip = 0;
        private int take = 25;
        private int count;
        private Filter filtering;
        private Sort sorting;
        private Query query;
        private SearchState state;

        public long TotalCount
        {
            get
            {
                //TODO: This isn't pritty, and we end up executing searches multiple times, 
                //      instead we should maintain an internal state that allows us to execute the search for counting, then reuse the result in that
                //      for materialization later if needed.
                //      but for now it will do.
                this.Any();

                return count;
            }
        }

        public IEnumerable<dynamic> Documents { get { return this.Select(hit => hit.Json); } }

        private int TotalTake => skip + take;

        public JsonSearchResult(Query query, IJsonIndexSearcher searcher)
        {
            this.state = new PendingSearch(this, query, searcher);

            this.searcher = searcher;
            this.query = query;

            results = new Lazy<IEnumerable<IJsonHit>>(ExecuteSearch);
        }

        public IEnumerator<IJsonHit> GetEnumerator()
        {
            return results.Value.GetEnumerator();
        }

        private IEnumerable<IJsonHit> ExecuteSearch()
        {
            Searcher searcher = null;//this.searcher.Aquire();
            query = searcher.Rewrite(query);

            TopDocs hits = sorting == null
                ? searcher.Search(query, filtering, TotalTake)
                : searcher.Search(query, filtering, TotalTake, sorting);

            count = hits.TotalHits;

            return hits.ScoreDocs.Skip(skip).Select(hit => new JsonHit(hit.Doc, hit.Score, searcher));
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal abstract class SearchState
        {
            protected JsonSearchResult Context { get; }

            public abstract int Count { get; }

            protected SearchState(JsonSearchResult context)
            {
                Context = context;
            }
        }

        internal class PendingSearch : SearchState
        {
            private readonly Query query;
            private readonly IJsonIndexSearcher searcher;

            public override int Count
            {
                get
                {
                    return ExecuteSearch();
                }
            }

            private int ExecuteSearch()
            {
                Searcher luceneSearcher = searcher.Aquire();
                Query q = luceneSearcher.Rewrite(query);

                TopDocs hits = Context.sorting == null
                    ? luceneSearcher.Search(query, Context.filtering, Context.TotalTake)
                    : luceneSearcher.Search(query, Context.filtering, Context.TotalTake, Context.sorting);

                Context.state = new CountedSearch(Context, hits, searcher);

                return hits.TotalHits;
            }

            public PendingSearch(JsonSearchResult context, Query query, IJsonIndexSearcher searcher) 
                : base(context)
            {
                this.query = query;
                this.searcher = searcher;
            }
        }

        internal class CountedSearch : SearchState
        {
            public override int Count { get; }

            public CountedSearch(JsonSearchResult context) 
                : base(context)
            {
            }

            public CountedSearch(JsonSearchResult context, TopDocs hits, IJsonIndexSearcher jsonIndexSearcher) : base(context)
            {
                
            }
        }

        internal class CompletedSearch : SearchState
        {
            public override int Count { get; }

            public CompletedSearch(JsonSearchResult context) 
                : base(context)
            {
            }
        }
    }


    public interface IJsonHit
    {
        float Score { get; }
        dynamic Json { get; }
        JObject Entity { get; }
    }

    public class JsonHit : IJsonHit
    {
        private readonly Searcher searcher;
        private Lazy<JObject> entity;
         
        public float Score { get; }

        public dynamic Json => Entity;
        public JObject Entity => entity.Value;

        public JsonHit(int doc, float score, Searcher searcher)
        {
            this.searcher = searcher;

            Score = score;

            entity = new Lazy<JObject>(() =>
            {
                Document document = searcher.Doc(doc, new MapFieldSelector("$raw"));
                return JObject.Parse(document.GetField("$raw").StringValue);
            });
        }


    }
}
