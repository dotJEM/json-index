using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Results
{
    public interface ISearchHit
    {
        float Score { get; }
        dynamic Json { get; }
        JObject Entity { get; }
    }

    public class SearchHit : ISearchHit
    {
        public SearchHit(float score, JObject entity)
        {
            Score = score;
            Entity = entity;
        }

        public float Score { get; }
        public dynamic Json => Entity;
        public JObject Entity { get; }
    }

    public sealed class Search : IEnumerable<ISearchHit>
    {
        private readonly IIndexSearcherManager manager;

        private readonly int skip, take;
        private readonly Query query;
        private readonly Filter filter;
        private readonly bool doDocScores;
        private readonly bool doMaxScores;
        private readonly Sort sort;
        public IInfoEventStream InfoStream { get; }

        public Guid CorrelationId { get; } = Guid.NewGuid();

        public Search Take(int newTake) => new Search(manager, InfoStream, query, skip, newTake, sort, filter, doDocScores, doMaxScores);
        public Search Skip(int newSkip) => new Search(manager, InfoStream, query, newSkip, take, sort, filter, doDocScores, doMaxScores);
        public Search Query(Query newQuery) => new Search(manager, InfoStream, newQuery, skip, take, sort, filter, doDocScores, doMaxScores);
        public Search OrderBy(Sort newSort) => new Search(manager, InfoStream, query, skip, take, newSort, filter, doDocScores, doMaxScores);
        public Search Filter(Filter newFilter) => new Search(manager, InfoStream, query, skip, take, sort, newFilter, doDocScores, doMaxScores);

        public Search WithoutDocScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, false, doMaxScores);
        public Search WithoutMaxScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, doDocScores, false);
        public Search WithoutScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, false, false);

        public Search WithDocScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, true, doMaxScores);
        public Search WithMaxScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, doDocScores, true);
        public Search WithScores() => new Search(manager, InfoStream, query, skip, take, sort, filter, true, true);

        public Search(IIndexSearcherManager manager, IInfoEventStream info, Query query = null, int skip = 0, int take = 25, Sort sort = null, Filter filter = null, bool doDocScores = true, bool doMaxScores = true)
        {
            this.manager = manager;
            this.InfoStream = info;
            this.skip = skip;
            this.take = take;
            this.query = query;
            this.filter = filter;
            this.doDocScores = doDocScores;
            this.doMaxScores = doMaxScores;
            this.sort = sort ?? Sort.RELEVANCE;
        }

        public Task<int> Count => Execute(query, 0, 1, null, filter, false, false).ContinueWith(t => t.Result.TotalHits);
        public Task<SearchResults> Result => Execute(query, skip, take, sort, filter, doDocScores, doMaxScores);

        private async Task<SearchResults> Execute(Query query, int skip, int take, Sort sort, Filter filter, bool doDocScores, bool doMaxScores)
        {
            using (IInfoStreamCorrelationScope scope = InfoStream.Scope(GetType(), CorrelationId))
            {
                scope.Debug($"Execute Search for query: {query}", new object[] { query, skip, take, sort, filter, doDocScores, doMaxScores });

                Stopwatch timer = Stopwatch.StartNew();
                using (IIndexSearcherContext context = manager.Acquire())
                {
                    IndexSearcher s = context.Searcher;
                    query = s.Rewrite(query);
                    scope.Debug($"Query Rewrite: {query}", new object[] { query });

                    // https://issues.apache.org/jira/secure/attachment/12430688/PagingCollector.java

                    TopFieldDocs results = await Task.Run(() => s.Search(query, filter, take, sort, doDocScores, doMaxScores));
                    TimeSpan searchTime = timer.Elapsed;
                    scope.Info($"Search took: {searchTime.TotalMilliseconds} ms", new object[] { searchTime });

                    var loaded =
                        from hit in results.ScoreDocs.Skip(skip)
                        let document = s.Doc(hit.Doc)
                        //TODO: Field Resolver
                        let data = document.GetBinaryValue("$$RAW").Bytes
                        select new { data, hit.Score };

                    //TODO: We could throw in another measurement (Load vs Deserialization)...
                    //      That would require us to force evaluate the above though (e.g. ToList it)....

                    ParallelQuery<ISearchHit> hits =
                        from hit in loaded.AsParallel().AsOrdered()
                        //TODO: Require Service
                        let json = new GZipJsonSerialier().Deserialize(hit.data)
                        select (ISearchHit)new SearchHit(hit.Score, json);
                    ISearchHit[] r = hits.ToArray();

                    TimeSpan loadTime = timer.Elapsed;
                    scope.Info($"Data load took: {loadTime.TotalMilliseconds} ms", new object[] { loadTime });
                    return new SearchResults(r, results.TotalHits);
                }
            }
        }

        public IEnumerator<ISearchHit> GetEnumerator()
        {
            return Result.ConfigureAwait(false).GetAwaiter().GetResult().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class SearchResults : IEnumerable<ISearchHit>
    {
        public ISearchHit[] Hits { get; }
        public int TotalHits { get; }

        public SearchResults(ISearchHit[] hits, int totalHits)
        {
            Hits = hits;
            TotalHits = totalHits;
        }
        
        public IEnumerator<ISearchHit> GetEnumerator() => Hits.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static implicit operator SearchResults(Search search) => search.Result.Result;
    }
}
