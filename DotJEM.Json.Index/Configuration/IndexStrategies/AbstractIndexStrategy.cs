using System;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IndexStrategies
{
    public abstract class AbstractIndexStrategy : IIndexStrategy
    {
        protected Field.Store FieldStore { get; private set; }
        protected Field.Index FieldIndex { get; private set; }
        protected Func<JValue, dynamic> Mapper { get; private set; }

        protected AbstractIndexStrategy()
        {
            FieldStore = Field.Store.NO;
            FieldIndex = Field.Index.NOT_ANALYZED;
            Mapper = token => token.ToString();
        }

        public AbstractIndexStrategy Map(Func<JValue, dynamic> func)
        {
            Mapper = func;
            return this;
        }

        internal AbstractIndexStrategy Stored(Field.Store store)
        {
            FieldStore = store;
            return this;
        }

        public AbstractIndexStrategy Analyzed(Field.Index index)
        {
            FieldIndex = index;
            return this;
        }

        public abstract IFieldable CreateField(string fieldName, JValue value);

    }
}