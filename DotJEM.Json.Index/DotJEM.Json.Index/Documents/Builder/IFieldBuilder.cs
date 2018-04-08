using System;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IFieldBuilder<out TValue>
    {
        IFieldBuilder<TValue> AddStringField();
        IFieldBuilder<TValue> AddStringField(Func<TValue, string> transform);
        IFieldBuilder<TValue> AddStringField(string extension, Func<TValue, string> transform);

        IFieldBuilder<TValue> AddTextField();
        IFieldBuilder<TValue> AddTextField(Func<TValue, string> transform);
        IFieldBuilder<TValue> AddTextField(string extension, Func<TValue, string> transform);

        IFieldBuilder<TValue> AddInt32Field();
        IFieldBuilder<TValue> AddInt32Field(Func<TValue, int> func);
        IFieldBuilder<TValue> AddInt32Field(string extension, Func<TValue, int> func);

        IFieldBuilder<TValue> AddInt64Field();
        IFieldBuilder<TValue> AddInt64Field(Func<TValue, long> func);
        IFieldBuilder<TValue> AddInt64Field(string extension, Func<TValue, long> func);

        IFieldBuilder<TValue> AddSingleField();
        IFieldBuilder<TValue> AddSingleField(Func<TValue, float> func);
        IFieldBuilder<TValue> AddSingleField(string extension, Func<TValue, float> func);

        IFieldBuilder<TValue> AddDoubleField();
        IFieldBuilder<TValue> AddDoubleField(Func<TValue, double> func);
        IFieldBuilder<TValue> AddDoubleField(string extension, Func<TValue, double> func);
    }
}