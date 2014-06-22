using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Configuration.IdentityStrategies;
using DotJEM.Json.Index.Configuration.IndexStrategies;
using DotJEM.Json.Index.Configuration.QueryStrategies;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public interface IStrategyResolver<out TStrategy> where TStrategy : class
    {
        TStrategy Strategy(string contentType, string field);
    }

    public interface IIndexConfiguration
    {
        IIndexConfiguration SetRawField(string field);
        IIndexConfiguration SetScoreField(string field);

        IIndexConfiguration SetTypeResolver(string field);
        IIndexConfiguration SetTypeResolver(IContentTypeResolver resolver);

        IContentTypeConfiguration ForAll();
        IContentTypeConfiguration For(string contentType);

        string RawField { get; }
        string ScoreField { get; }
        IContentTypeResolver TypeResolver { get; }
        IStrategyResolver<IIndexStrategy> Index { get; }
        IStrategyResolver<IQueryStrategy> Query { get; }
        IStrategyResolver<IIdentityStrategy> Identity { get; }
    }

    public class IndexConfiguration : IIndexConfiguration
    {
        private readonly IDictionary<string, IContentTypeConfiguration> configurations
            = new Dictionary<string, IContentTypeConfiguration>(StringComparer.InvariantCultureIgnoreCase);

        public string RawField { get; private set; }
        public string ScoreField { get; private set; }
        public IContentTypeResolver TypeResolver { get; private set; }

        public IndexConfiguration()
        {
            SetTypeResolver("$contentType");
            SetRawField("$raw");
            SetScoreField("$score");
        }

        public string ResolveIdentity(JObject value)
        {
            return For(TypeResolver.Resolve(value)).IndentityStrategy.Resolve(value);
        }

        public IIndexConfiguration SetTypeResolver(string field)
        {
            For(string.Empty)
                .Index(field, As.Default().Analyzed(Field.Index.NOT_ANALYZED))
                .Query(field, Using.Term().When.Specified());

            return SetTypeResolver(new FieldContentTypeResolver(field));
        }

        public IIndexConfiguration SetTypeResolver(IContentTypeResolver resolver)
        {
            TypeResolver = resolver;
            return this;
        }

        public IIndexConfiguration SetRawField(string field)
        {
            RawField = field;
            return this;
        }

        public IIndexConfiguration SetScoreField(string field)
        {
            ScoreField = field;
            return this;
        }

        #region ILuceneConfigurationBuilder

        public IContentTypeConfiguration ForAll()
        {
            return For(string.Empty);
        }

        public IContentTypeConfiguration For(string contentType)
        {
            if (!configurations.ContainsKey(contentType))
                configurations[contentType] = new ContentTypeConfiguration();
            return configurations[contentType];
        }

        #endregion

        #region IIndexConfiguration

        public IStrategyResolver<IIdentityStrategy> Identity
        {
            get
            {
                return new StrategyResolver<IIdentityStrategy, DefaultIdentityStrategy>(
                    (fullName, contentType) => For(contentType).IndentityStrategy);
            }
        }

        public IStrategyResolver<IIndexStrategy> Index
        {
            get
            {
                return new StrategyResolver<IIndexStrategy, DefaultIndexStrategy>(
                    (fullName, contentType) => For(contentType).GetIndexStrategy(fullName));
            }
        }

        public IStrategyResolver<IQueryStrategy> Query
        {
            get
            {
                return new StrategyResolver<IQueryStrategy, DefaultQueryStrategy>(
                    (fullName, contentType) => For(contentType).GetQueryStrategy(fullName));
            }
        }

        private class StrategyResolver<TStrategy, TDefault> : IStrategyResolver<TStrategy>
            where TStrategy : class
            where TDefault : TStrategy, new()
        {
            private readonly Func<string, string, TStrategy> resolve;

            public StrategyResolver(Func<string, string, TStrategy> resolve)
            {
                this.resolve = resolve;
            }

            public TStrategy Strategy(string contentType, string field)
            {
                TStrategy strategy = resolve(field, contentType) ?? resolve(field, string.Empty);
                if (strategy != null)
                    return strategy;
                return new TDefault();
            }
        }

        #endregion
    }
}