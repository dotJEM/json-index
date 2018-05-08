using System;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public static class NumberFieldFactoryExtensions
    {
        public static IJsonIndexableField CreateInt32Field<TValue>(this IFieldFactory<TValue> self)
            => self.CreateInt32Field(val => Convert.ToInt32(val));

        public static IJsonIndexableField CreateInt32Field<TValue>(this IFieldFactory<TValue> self, Func<TValue, int> transform)
            => self.InternalCreateInt32Field(self.Context.Field, transform);

        public static IJsonIndexableField CreateInt32Field<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, int> transform)
            => self.InternalCreateInt32Field($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateInt32Field<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, int> transform)
        {
            return new JsonIndexableField<int>(
                new Int32Field(fullpath, transform(self.DeserializedValue), Field.Store.NO)
            );
        }

        public static IJsonIndexableField CreateInt64Field<TValue>(this IFieldFactory<TValue> self)
            => self.CreateInt64Field(val => Convert.ToInt64(val));

        public static IJsonIndexableField CreateInt64Field<TValue>(this IFieldFactory<TValue> self, Func<TValue, long> transform)
            => self.InternalCreateInt64Field(self.Context.Field, transform);

        public static IJsonIndexableField CreateInt64Field<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, long> transform)
            => self.InternalCreateInt64Field($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateInt64Field<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, long> transform)
        {
            return new JsonIndexableField<long>(
                new Int64Field(fullpath, transform(self.DeserializedValue), Field.Store.NO)
            );
        }

        public static IJsonIndexableField CreateSingleField<TValue>(this IFieldFactory<TValue> self)
            => self.CreateSingleField(val => Convert.ToSingle(val));

        public static IJsonIndexableField CreateSingleField<TValue>(this IFieldFactory<TValue> self, Func<TValue, float> transform)
            => self.InternalCreateSingleField(self.Context.Field, transform);

        public static IJsonIndexableField CreateSingleField<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, float> transform)
            => self.InternalCreateSingleField($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateSingleField<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, float> transform)
        {
            return new JsonIndexableField<float>(
                new SingleField(fullpath, transform(self.DeserializedValue), Field.Store.NO)
            );
        }

        public static IJsonIndexableField CreateDoubleField<TValue>(this IFieldFactory<TValue> self)
            => self.CreateDoubleField(val => Convert.ToSingle(val));

        public static IJsonIndexableField CreateDoubleField<TValue>(this IFieldFactory<TValue> self, Func<TValue, double> transform)
            => self.InternalCreateDoubleField(self.Context.Field, transform);

        public static IJsonIndexableField CreateDoubleField<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, double> transform)
            => self.InternalCreateDoubleField($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateDoubleField<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, double> transform)
        {
            return new JsonIndexableField<float>(
                new DoubleField(fullpath, transform(self.DeserializedValue), Field.Store.NO)
            );
        }
    }
}