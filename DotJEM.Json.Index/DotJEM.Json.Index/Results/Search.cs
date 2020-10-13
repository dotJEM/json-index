using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Results
{
    public interface ISearchResult
    {
        float Score { get; }
        dynamic Json { get; }
        JObject Entity { get; }
    }

    public class SearchResult : ISearchResult
    {
        private readonly ScoreDoc score;
        private readonly JObject entity;

        public float Score => score.Score;
        public dynamic Json => entity;
        public JObject Entity => entity;

        public SearchResult(ScoreDoc score, JObject entity)
        {
            this.score = score;
            this.entity = entity;
        }

    }

    public interface ISearch
    {
        IEventInfoStream EventInfoStream { get; }
        Guid CorrelationId { get; }
        Task<int> Count { get; }
        ISearch Take(int newTake);
        ISearch Skip(int newSkip);
        ISearch Query(Query newQuery);
        ISearch OrderBy(Sort newSort);
        ISearch Filter(Filter newFilter);
        ISearch WithoutDocScores();
        ISearch WithoutMaxScores();
        ISearch WithoutScores();
        ISearch WithDocScores();
        ISearch WithMaxScores();
        ISearch WithScores();
        Task<SearchResults> Execute();
    }

    public sealed class Search : ISearch
    {
        private readonly IIndexSearcherManager manager;

        private readonly int skip, take;
        private readonly Query query;
        private readonly Filter filter;
        private readonly bool doDocScores;
        private readonly bool doMaxScores;
        private readonly Sort sort;

        public IEventInfoStream EventInfoStream { get; }

        public Guid CorrelationId { get; } = Guid.NewGuid();
        public ISearch Take(int newTake) => new Search(manager, EventInfoStream, query, skip, newTake, sort, filter, doDocScores, doMaxScores);
        public ISearch Skip(int newSkip) => new Search(manager, EventInfoStream, query, newSkip, take, sort, filter, doDocScores, doMaxScores);
        public ISearch Query(Query newQuery) => new Search(manager, EventInfoStream, newQuery, skip, take, sort, filter, doDocScores, doMaxScores);
        public ISearch OrderBy(Sort newSort) => new Search(manager, EventInfoStream, query, skip, take, newSort, filter, doDocScores, doMaxScores);
        public ISearch Filter(Filter newFilter) => new Search(manager, EventInfoStream, query, skip, take, sort, newFilter, doDocScores, doMaxScores);

        public ISearch WithoutDocScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, false, doMaxScores);
        public ISearch WithoutMaxScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, doDocScores, false);
        public ISearch WithoutScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, false, false);

        public ISearch WithDocScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, true, doMaxScores);
        public ISearch WithMaxScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, doDocScores, true);
        public ISearch WithScores() => new Search(manager, EventInfoStream, query, skip, take, sort, filter, true, true);

        public Search(IIndexSearcherManager manager, IEventInfoStream eventInfo, Query query = null, int skip = 0, int take = 25, Sort sort = null, Filter filter = null, bool doDocScores = true, bool doMaxScores = true)
        {
            this.manager = manager;
            this.EventInfoStream = eventInfo;
            this.skip = skip;
            this.take = take;
            this.query = query;
            this.filter = filter;
            this.doDocScores = doDocScores;
            this.doMaxScores = doMaxScores;
            this.sort = sort ?? Sort.RELEVANCE;
        }

        public Task<int> Count => Execute(query, 0, 1, null, filter, false, false).ContinueWith(t => t.Result.TotalHits);
        public Task<SearchResults> Execute() => Execute(query, skip, take, sort, filter, doDocScores, doMaxScores);

        private async Task<SearchResults> Execute(Query query, int skip, int take, Sort sort, Filter filter, bool doDocScores, bool doMaxScores)
        {
            await Task.Yield();
            using (IInfoStreamCorrelationScope scope = EventInfoStream.Scope(typeof(Search), CorrelationId))
            {
                scope.Debug($"Execute Search for query: {query}", new object[] { query, skip, take, sort, filter, doDocScores, doMaxScores });

                Stopwatch timer = Stopwatch.StartNew();
                using (IIndexSearcherContext context = manager.Acquire())
                {
                    IndexSearcher searcher = context.Searcher;
                    //s.Doc()
                    //query = s.Rewrite(query);
                    scope.Debug($"Query Rewrite: {query}", new object[] { query });

                    // https://issues.apache.org/jira/secure/attachment/12430688/PagingCollector.java

                    //TopScoreDocCollector collector = TopScoreDocCollector.Create(int.MaxValue, true);
                    //TopDocs docs = collector.GetTopDocs(0, 100);
                    //TopFieldCollector collector2 = TopFieldCollector.Create(sort, 100, false, false, false, false);
                    //Query fq = filter != null 
                    //    ? new FilteredQuery(query, filter)
                    //    : query;
                    //Weight w = s.CreateNormalizedWeight(fq);
                    //collector2.GetTopDocs()
                    ILuceneJsonDocumentSerializer serializer = manager.Serializer;

                    TopFieldDocs topDocs = searcher.Search(query, filter, take, sort, doDocScores, doMaxScores);
                    
                    TimeSpan searchTime = timer.Elapsed;
                    scope.Info($"Search took: {searchTime.TotalMilliseconds} ms", new object[] { searchTime });
                    ParallelQuery<SearchResult> loaded =
                        from hit in topDocs.ScoreDocs.Skip(skip).AsParallel().AsOrdered()
                        let document = searcher.Doc(hit.Doc, serializer.FieldsToLoad)
                        select new SearchResult(hit, serializer.DeserializeFrom(document));

                    //TODO: We could throw in another measurement (Load vs Deserialization)...
                    //      That would require us to force evaluate the above though (e.g. ToList it)....

                    ISearchResult[] results = loaded.Cast<ISearchResult>().ToArray();

                    TimeSpan loadTime = timer.Elapsed;
                    scope.Info($"Data load took: {loadTime.TotalMilliseconds} ms", new object[] { loadTime });
                    return new SearchResults(results, topDocs.TotalHits);
                }
            }
        }
    }

    public class SearchResults : IEnumerable<ISearchResult>
    {
        public ISearchResult[] Hits { get; }
        public int TotalHits { get; }

        public SearchResults(ISearchResult[] hits, int totalHits)
        {
            Hits = hits;
            TotalHits = totalHits;
        }
        
        public IEnumerator<ISearchResult> GetEnumerator() => Hits.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
