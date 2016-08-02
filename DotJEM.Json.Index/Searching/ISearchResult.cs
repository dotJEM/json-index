using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface ISearchResult : IEnumerable<IHit>
    {
        long TotalCount { get; }
        TimeSpan SearchTime { get; }
        TimeSpan LoadTime { get; }
        TimeSpan TotalTime { get; }
        Query Query { get; }

        IEnumerable<dynamic> Documents { get; }

        ISearchResult Take(int count);
        ISearchResult Skip(int count);
        
        //TODO: Filter Builder instead.
        ISearchResult Filter(Filter filtering);

        //TODO: Sort Builder instead.
        ISearchResult Sort(Sort sorting);

        //TODO: Sort Builder instead.
        ISearchResult All();
    }

    public class SearchResultCollector : ISearchResult
    {
        private int take = 25, skip;
        private Filter filtering;
        private Sort sorting;
        private IndexSearcher searcher;

        private readonly IStorageIndex index;

        public long TotalCount { get; private set; }
        public TimeSpan SearchTime { get; private set; }
        public TimeSpan TotalTime { get; private set; }
        public TimeSpan LoadTime => TotalTime - SearchTime;
        public Query Query { get; }

        public IEnumerable<dynamic> Documents { get { return this.Select(hit => hit.Json); } }


        public SearchResultCollector(Query query, IStorageIndex index)
        {
            this.Query = query;
            this.index = index;
        }
        
        private JObject ResolveJObject(IHit hit)
        {
            return index.Configuration.Serializer.Deserialize(index.Configuration.RawField, searcher.Doc(hit.Doc));
        }

        public ISearchResult Take(int count)
        {
            take = count;
            return this;
        }

        public ISearchResult Skip(int count)
        {
            skip = count;
            return this;
        }

        public ISearchResult Filter(Filter value)
        {
            filtering = value;
            return this;
        }
        
        public ISearchResult Sort(Sort value)
        {
            sorting = value;
            return this;
        }

        public ISearchResult All()
        {
            take = int.MaxValue;
            return this;
        }

        public IEnumerator<IHit> GetEnumerator()
        {
            if(!index.Storage.Exists)
                yield break;

            Stopwatch timer = Stopwatch.StartNew();
            using (searcher = new IndexSearcher(index.Storage.OpenReader()))
            {
                Query query = searcher.Rewrite(Query);
                TopDocs hits = sorting == null
                    ? searcher.Search(query, filtering, take + skip)
                    : searcher.Search(query, filtering, take + skip, sorting);
                TotalCount = hits.TotalHits;

                SearchTime = timer.Elapsed;

                foreach (ScoreDoc hit in hits.ScoreDocs.Skip(skip))
                    yield return new Hit(hit.Doc, hit.Score, ResolveJObject);
            }
            TotalTime = timer.Elapsed;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}