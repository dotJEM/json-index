using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Configuration.FieldStrategies;
using DotJEM.Json.Index.Configuration.IdentityStrategies;

namespace DotJEM.Json.Index.Configuration
{
    /// <summary>
    /// Provides the means to extend the framework with extension methods and share a common syntax.
    /// If custom implementations are not needed, it is recommended just to use the static <see cref="As"/> class.
    /// </summary>
    public interface IFieldStrategyBuilder { }

    public interface IContentTypeConfiguration
    {
        IFieldStrategy this[string key] { get; }
        IIdentityResolver IndentityResolver { get; }

        IContentTypeConfiguration Index(string field, IFieldStrategy strategy);
        IContentTypeConfiguration Index(string field, Func<IFieldStrategyBuilder, IFieldStrategy> build);

        IContentTypeConfiguration SetIdentity(string field);
        IContentTypeConfiguration SetIdentity(IIdentityResolver resolver);

        IFieldStrategy GetStrategy(string fullName);
    }

    public class ContentTypeConfiguration : IContentTypeConfiguration
    {
        private readonly IDictionary<string, IFieldStrategy> strategies = new Dictionary<string, IFieldStrategy>();

        public IIdentityResolver IndentityResolver { get; private set; }

        public IFieldStrategy this[string key]
        {
            get
            {
                IFieldStrategy strategy;
                strategies.TryGetValue(key, out strategy);
                return strategy;
            }
        }

        public IContentTypeConfiguration SetIdentity(string field)
        {
            return SetIdentity(new GuidIdentity(field))
                .Index(field, As.Term);
        }

        public IContentTypeConfiguration SetIdentity(IIdentityResolver resolver)
        {
            IndentityResolver = resolver;
            return this;
        }

        public IContentTypeConfiguration Index(string field, IFieldStrategy strategy)
        {
            strategies[field] = strategy;
            return this;
        }

        public IContentTypeConfiguration Index(string field, Func<IFieldStrategyBuilder, IFieldStrategy> build)
        {
            return Index(field, build(DummyStrategyBuilder.Instance));
        }

        public IFieldStrategy GetStrategy(string fullName)
        {
            return this[fullName];
        }

        /// <summary>
        /// Dummy implementation of IIndexStrategyBuilder and IQueryStrategyBuilder.
        /// Since these interfaces are just meant to guide intelisense for extension methods, we 
        /// don't need an actual implementation.
        /// </summary>
        private class DummyStrategyBuilder : IFieldStrategyBuilder
        {
            internal static readonly DummyStrategyBuilder Instance = new DummyStrategyBuilder();
        }
    }

    public class MultiTargetContentTypeConfiguration : IContentTypeConfiguration
    {
        private readonly IEnumerable<IContentTypeConfiguration> targets;

        public MultiTargetContentTypeConfiguration(IEnumerable<IContentTypeConfiguration> targets)
        {
            this.targets = targets;
        }

        public IFieldStrategy GetStrategy(string fullName)
        {
            throw new NotSupportedException();
        }

        public IIdentityResolver IndentityResolver
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public IContentTypeConfiguration Index(string field, IFieldStrategy strategy)
        {
            targets.AsParallel().ForAll(x => x.Index(field, strategy));
            return this;
        }

        public IContentTypeConfiguration Index(string field, Func<IFieldStrategyBuilder, IFieldStrategy> build)
        {
            targets.AsParallel().ForAll(x => x.Index(field, build));
            return this;
        }

        public IFieldStrategy this[string key]
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public IContentTypeConfiguration SetIdentity(string field)
        {
            targets.AsParallel().ForAll(x => x.SetIdentity(field));
            return this;
        }

        public IContentTypeConfiguration SetIdentity(IIdentityResolver resolver)
        {
            targets.AsParallel().ForAll(x => x.SetIdentity(resolver));
            return this;
        }
    }

}