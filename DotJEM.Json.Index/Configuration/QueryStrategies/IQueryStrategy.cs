using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.QueryStrategies
{
    public interface IQueryStrategy
    {
        IFieldQueryWhenConfiguration When { get; }

        Query Create(string field, string value);
    }

    public abstract class AbstactQueryStrategy : IQueryStrategy
    {
        public IFieldQueryWhenConfiguration When { get; private set; }

        protected AbstactQueryStrategy()
        {
            When = new FieldQueryWhenConfiguration(this);
        }

        public abstract Query Create(string field, string value);
    }

    public interface IFieldQueryWhenConfiguration
    {
        IQueryStrategy Specified();
        IQueryStrategy Always();
    }

    public class FieldQueryWhenConfiguration : IFieldQueryWhenConfiguration
    {
        private readonly IQueryStrategy parent;

        public FieldQueryWhenConfiguration(IQueryStrategy parent)
        {
            this.parent = parent;
        }

        public IQueryStrategy Specified()
        {
            return parent;
        }

        public IQueryStrategy Always()
        {
            return parent;
        }
    }
}