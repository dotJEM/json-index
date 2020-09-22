using System;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public static class NumberJsonIndexableFieldBuilderExtensions
    {
        public static IJsonIndexableFieldBuilder<TValue> CreateInt32Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateInt32Field(val => Convert.ToInt32(val));

        public static IJsonIndexableFieldBuilder<TValue> CreateInt32Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, int> transform)
            => self.InternalCreateInt32Field(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateInt32Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, int> transform)
            => self.InternalCreateInt32Field($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateInt32Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, int> transform)
            => self.Add(new Int32Field(fullpath, transform(self.DeserializedValue), Field.Store.NO));

        public static IJsonIndexableFieldBuilder<TValue> CreateInt64Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateInt64Field(val => Convert.ToInt64(val));

        public static IJsonIndexableFieldBuilder<TValue> CreateInt64Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, long> transform)
            => self.InternalCreateInt64Field(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateInt64Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, long> transform)
            => self.InternalCreateInt64Field($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateInt64Field<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, long> transform)
            => self.Add(new Int64Field(fullpath, transform(self.DeserializedValue), Field.Store.NO));

        public static IJsonIndexableFieldBuilder<TValue> CreateSingleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateSingleField(val => Convert.ToSingle(val));

        public static IJsonIndexableFieldBuilder<TValue> CreateSingleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, float> transform)
            => self.InternalCreateSingleField(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateSingleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, float> transform)
            => self.InternalCreateSingleField($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateSingleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, float> transform)
            => self.Add(new SingleField(fullpath, transform(self.DeserializedValue), Field.Store.NO));

        public static IJsonIndexableFieldBuilder<TValue> CreateDoubleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateDoubleField(val => Convert.ToSingle(val));

        public static IJsonIndexableFieldBuilder<TValue> CreateDoubleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, double> transform)
            => self.InternalCreateDoubleField(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateDoubleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, double> transform)
            => self.InternalCreateDoubleField($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateDoubleField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, double> transform)
            => self.Add(new DoubleField(fullpath, transform(self.DeserializedValue), Field.Store.NO));
    }
}