using System;
using DotJEM.Json.Index.Configuration.IndexStrategies;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public static class As
    {
        public static NumericIndexStrategy Integer()
        {
            return new NumericIndexStrategy((field, value) => field.SetIntValue(value.Value<int>()));
        }

        public static NumericIndexStrategy Long()
        {
            return new NumericIndexStrategy((field, value) => field.SetLongValue(value.Value<long>()));
        }
        
        public static NumericIndexStrategy Float()
        {
            return new NumericIndexStrategy((field, value) => field.SetFloatValue(value.Value<float>()));
        }

        public static NumericIndexStrategy Double()
        {
            return new NumericIndexStrategy((field, value) => field.SetDoubleValue(value.Value<double>()));
        }

        public static NumericIndexStrategy DateTime()
        {
            return new NumericIndexStrategy((field, value) => field.SetLongValue(value.Value<DateTime>().Ticks));
        }

        public static DefaultIndexStrategy Nothing()
        {
            return new DefaultIndexStrategy();
        }

        public static DefaultIndexStrategy Default()
        {
            return new DefaultIndexStrategy();
        }

        public static AbstractIndexStrategy Term()
        {
            return new GenericStringIndexStrategy();
        }

        public static AbstractIndexStrategy Analyzed()
        {
            return new GenericStringIndexStrategy().Analyzed(Field.Index.ANALYZED);
        }

        internal static AbstractIndexStrategy Stored()
        {
            return new GenericStringIndexStrategy().Stored(Field.Store.YES);
        }
    }

    public static class IndexStrategiesExt
    {
        public static NumericIndexStrategy Integer(this IIndexStrategyBuilder self)
        {
            return As.Integer();
        }

        public static NumericIndexStrategy Long(this IIndexStrategyBuilder self)
        {
            return As.Long();
        }

        public static NumericIndexStrategy Float(this IIndexStrategyBuilder self)
        {
            return As.Float();
        }

        public static NumericIndexStrategy Double(this IIndexStrategyBuilder self)
        {
            return As.Double();
        }

        public static NumericIndexStrategy DateTime(this IIndexStrategyBuilder self)
        {
            return As.DateTime();
        }

        public static DefaultIndexStrategy Nothing(this IIndexStrategyBuilder self)
        {
            return As.Nothing();
        }

        public static DefaultIndexStrategy Default(this IIndexStrategyBuilder self)
        {
            return As.Default();
        }
    }
}