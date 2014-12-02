using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Configuration.IdentityStrategies;
using DotJEM.Json.Index.Configuration.IndexStrategies;
using DotJEM.Json.Index.Configuration.QueryStrategies;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Configuration
{
    /// <summary>
    /// Provides the means to extend the framework with extension methods and share a common syntax.
    /// If custom implementations are not needed, it is recommended just to use the static <see cref="As"/> class.
    /// </summary>
    public interface IQueryStrategyBuilder { }

    /// <summary>
    /// Provides the means to extend the framework with extension methods and share a common syntax.
    /// If custom implementations are not needed, it is recommended just to use the static <see cref="Using"/> class.
    /// </summary>
    public interface IIndexStrategyBuilder { }

    public interface IContentTypeConfiguration
    {
        IIdentityStrategy IndentityStrategy { get; }

        IContentTypeConfiguration Index(string field, IIndexStrategy indexStrategy);
        IContentTypeConfiguration Index(string field, Func<IIndexStrategyBuilder, IIndexStrategy> build);
        IContentTypeConfiguration Query(string field, IQueryStrategy indexQueryStrategy);
        IContentTypeConfiguration Query(string field, Func<IQueryStrategyBuilder, IQueryStrategy> build);

        IContentTypeConfiguration SetIdentity(string field);
        IContentTypeConfiguration SetIdentity(IIdentityStrategy strategy);

        IIndexStrategy GetIndexStrategy(string fullName);
        IQueryStrategy GetQueryStrategy(string fullName);
    }

    public class ContentTypeConfiguration : IContentTypeConfiguration
    {
        private readonly IDictionary<string, IIndexStrategy> indexConfigurations = new Dictionary<string, IIndexStrategy>();
        private readonly IDictionary<string, IQueryStrategy> queryConfigurations = new Dictionary<string, IQueryStrategy>();

        public IIdentityStrategy IndentityStrategy { get; private set; }

        public IContentTypeConfiguration SetIdentity(string field)
        {
            return SetIdentity(new GuidIdentity(field))
                .Index(field, As.Stored().Analyzed(Field.Index.NOT_ANALYZED))
                .Query(field, Using.Term().When.Specified());
        }

        public IContentTypeConfiguration SetIdentity(IIdentityStrategy strategy)
        {
            IndentityStrategy = strategy;
            return this;
        }

        public IIndexStrategy GetIndexStrategy(string fullName)
        {
            return indexConfigurations.ContainsKey(fullName) ? indexConfigurations[fullName] : null;
        }

        public IQueryStrategy GetQueryStrategy(string fullName)
        {
            return queryConfigurations.ContainsKey(fullName) ? queryConfigurations[fullName] : null;
        }

        public IContentTypeConfiguration Index(string field, IIndexStrategy indexStrategy)
        {
            indexConfigurations[field] = indexStrategy;
            return this;
        }

        public IContentTypeConfiguration Index(string field, Func<IIndexStrategyBuilder, IIndexStrategy> build)
        {
            return Index(field, build(DummyStrategyBuilder.Instance));
        }

        public IContentTypeConfiguration Query(string field, IQueryStrategy indexQueryStrategy)
        {
            queryConfigurations[field] = indexQueryStrategy;
            return this;
        }

        public IContentTypeConfiguration Query(string field, Func<IQueryStrategyBuilder, IQueryStrategy> build)
        {
            return Query(field, build(DummyStrategyBuilder.Instance));
        }

        /// <summary>
        /// Dummy implementation of IIndexStrategyBuilder and IQueryStrategyBuilder.
        /// Since these interfaces are just meant to guide intelisense for extension methods, we 
        /// don't need an actual implementation.
        /// </summary>
        private class DummyStrategyBuilder : IIndexStrategyBuilder, IQueryStrategyBuilder
        {
            internal static readonly DummyStrategyBuilder Instance = new DummyStrategyBuilder();
        }
    }


}