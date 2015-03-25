using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Configuration.FieldStrategies;
using DotJEM.Json.Index.Configuration.IdentityStrategies;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public interface IStrategyResolver<out TStrategy> where TStrategy : class
    {
        TStrategy Strategy(string contentType, string field);
        TStrategy Strategy(string field);
    }

    public interface IIndexConfiguration
    {
        IIndexConfiguration SetRawField(string field);
        IIndexConfiguration SetScoreField(string field);
        IIndexConfiguration SetTypeResolver(string field);
        IIndexConfiguration SetTypeResolver(IContentTypeResolver resolver);
        IIndexConfiguration SetIdentity(string field);
        IIndexConfiguration SetIdentity(IIdentityStrategy strategy);

        IContentTypeConfiguration ForAll();
        IContentTypeConfiguration For(string contentType);
        //IContentTypeConfiguration For(params string[] contentType);

        string RawField { get; }
        string ScoreField { get; }
        IContentTypeResolver TypeResolver { get; }
        IIdentityStrategy IdentityStrategy { get; }
        IStrategyResolver<IFieldStrategy> Field { get; }
    }

    public class IndexConfiguration : IIndexConfiguration
    {
        private readonly IDictionary<string, IContentTypeConfiguration> configurations
            = new Dictionary<string, IContentTypeConfiguration>(StringComparer.InvariantCultureIgnoreCase);

        public string RawField { get; private set; }
        public string ScoreField { get; private set; }

        public IContentTypeResolver TypeResolver { get; private set; }
        public IIdentityStrategy IdentityStrategy { get { return ForAll().IndentityStrategy; } }

        public IndexConfiguration()
        {
            SetRawField("$raw");
            SetScoreField("$score");
            SetTypeResolver("$contentType");
            SetIdentity("$id");
        }

        public string ResolveIdentity(JObject value)
        {
            return For(TypeResolver.Resolve(value)).IndentityStrategy.Resolve(value);
        }

        public IIndexConfiguration SetTypeResolver(string field)
        {
            ForAll().Index(field, As.Term);

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

        public IIndexConfiguration SetIdentity(string field)
        {
            ForAll().SetIdentity(field);
            return this;
        }

        public IIndexConfiguration SetIdentity(IIdentityStrategy strategy)
        {
            ForAll().SetIdentity(strategy);
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

        public IStrategyResolver<IFieldStrategy> Field
        {
            get
            {
                return new StrategyResolver<IFieldStrategy, FieldStrategy>((fullName, contentType) => For(contentType).GetStrategy(fullName));
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

            public TStrategy Strategy(string field)
            {
                return resolve(field, string.Empty) ?? new TDefault();
            }

            public TStrategy Strategy(string contentType, string field)
            {
                return resolve(field, contentType) ?? Strategy(field);
            }
        }

        #endregion
    }
}