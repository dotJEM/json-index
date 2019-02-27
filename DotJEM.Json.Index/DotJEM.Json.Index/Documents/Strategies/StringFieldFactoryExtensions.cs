using System;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public static class StringFieldFactoryExtensions
    {
        public static IJsonIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self)
            => self.CreateStringField(val => val.ToString());

        public static IJsonIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self, Func<TValue, string> transform)
            => self.InternalCreateStringField(self.Context.Field, transform);

        public static IJsonIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, string> transform)
            => self.InternalCreateStringField($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateStringField<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, string> transform)
            => new JsonIndexableField<string>(new StringField(fullpath, transform(self.DeserializedValue), Field.Store.NO));

        public static IJsonIndexableField CreateTextField<TValue>(this IFieldFactory<TValue> self)
            => self.CreateTextField(val => val.ToString());

        public static IJsonIndexableField CreateTextField<TValue>(this IFieldFactory<TValue> self, Func<TValue, string> transform)
            => self.InternalCreateTextField(self.Context.Field, transform);

        public static IJsonIndexableField CreateTextField<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, string> transform)
            => self.InternalCreateTextField($"{self.Context.Field}.{extension}", transform);

        private static IJsonIndexableField InternalCreateTextField<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, string> transform)
            => new JsonIndexableField<string>(new TextField(fullpath, transform(self.DeserializedValue), Field.Store.NO));
    }
}