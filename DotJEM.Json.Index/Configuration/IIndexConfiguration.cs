using System;
using System.Collections.Generic;
using System.Linq;
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
        IIndexConfiguration SetTypeResolver(IFieldResolver resolver);
        IIndexConfiguration SetIdentity(string field);
        IIndexConfiguration SetIdentity(IIdentityResolver resolver);

        IContentTypeConfiguration ForAll();
        IContentTypeConfiguration For(params string[] contentType);

        string RawField { get; }
        string ScoreField { get; }

        IFieldResolver TypeResolver { get; }
        IFieldResolver AreaResolver { get; }

        IIdentityResolver IdentityResolver { get; }
        IStrategyResolver<IFieldStrategy> Field { get; }
    }

    public class IndexConfiguration : IIndexConfiguration
    {
        private readonly IDictionary<string, IContentTypeConfiguration> configurations
            = new Dictionary<string, IContentTypeConfiguration>(StringComparer.InvariantCultureIgnoreCase);

        public string RawField { get; private set; }
        public string ScoreField { get; private set; }

        public IFieldResolver TypeResolver { get; private set; }
        public IFieldResolver AreaResolver { get; private set; }

        public IIdentityResolver IdentityResolver { get { return ForAll().IndentityResolver; } }

        public IndexConfiguration()
        {
            SetRawField("$raw");
            SetScoreField("$score");
            SetTypeResolver("$contentType");
            SetAreaResolver("$area");
            SetIdentity("$id");
        }

        public string ResolveIdentity(JObject value)
        {
            return For(TypeResolver.Resolve(value)).IndentityResolver.Resolve(value);
        }

        #region Set Methods
        public IIndexConfiguration SetTypeResolver(string field)
        {
            ForAll().Index(field, As.Term);

            return SetTypeResolver(new FieldResolver(field));
        }

        public IIndexConfiguration SetAreaResolver(string field)
        {
            ForAll().Index(field, As.Term);

            return SetAreaResolver(new FieldResolver(field));
        }

        public IIndexConfiguration SetTypeResolver(IFieldResolver resolver)
        {
            TypeResolver = resolver;
            return this;
        }

        public IIndexConfiguration SetAreaResolver(IFieldResolver resolver)
        {
            AreaResolver = resolver;
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

        public IIndexConfiguration SetIdentity(IIdentityResolver resolver)
        {
            ForAll().SetIdentity(resolver);
            return this;
        } 
        #endregion

        #region ILuceneConfigurationBuilder

        public IContentTypeConfiguration ForAll()
        {
            return InternalFor(string.Empty);
        }

        private IContentTypeConfiguration InternalFor(string contentType)
        {
            if (!configurations.ContainsKey(contentType))
                configurations[contentType] = new ContentTypeConfiguration();

            return configurations[contentType];
        }

        public IContentTypeConfiguration For(params string[] contentTypes)
        {
            if (contentTypes.Length == 0)
            {
                return ForAll();
            }

            if (contentTypes.Length == 1)
            {
                return InternalFor(contentTypes[0]);
            }

            return new MultiTargetContentTypeConfiguration(contentTypes.Select(InternalFor));
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