using System;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public static class StringJsonIndexableFieldBuilderExtensions
    {
        public static IJsonIndexableFieldBuilder<TValue> CreateStringField<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateStringField(val => val.ToString());

        public static IJsonIndexableFieldBuilder<TValue> CreateStringField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, string> transform)
            => self.InternalCreateStringField(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateStringField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, string> transform)
            => self.InternalCreateStringField($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateStringField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, string> transform)
            => self.Add(new StringField(fullpath, transform(self.DeserializedValue), Field.Store.NO));

        public static IJsonIndexableFieldBuilder<TValue> CreateTextField<TValue>(this IJsonIndexableFieldBuilder<TValue> self)
            => self.CreateTextField(val => val.ToString());

        public static IJsonIndexableFieldBuilder<TValue> CreateTextField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, Func<TValue, string> transform)
            => self.InternalCreateTextField(self.Context.Path, transform);

        public static IJsonIndexableFieldBuilder<TValue> CreateTextField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string extension, Func<TValue, string> transform)
            => self.InternalCreateTextField($"{self.Context.Path}.{extension}", transform);

        private static IJsonIndexableFieldBuilder<TValue> InternalCreateTextField<TValue>(this IJsonIndexableFieldBuilder<TValue> self, string fullpath, Func<TValue, string> transform)
            => self.Add(new TextField(fullpath, transform(self.DeserializedValue), Field.Store.NO));
    }
}