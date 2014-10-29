using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IndexStrategies
{
    public class NullIndexStrategy : AbstractIndexStrategy
    {
        public override IEnumerable<IFieldable> CreateField(string fieldName, JValue value)
        {
            return Enumerable.Empty<IFieldable>();
        }
    }

    public class GenericStringIndexStrategy : AbstractIndexStrategy
    {
        public override IEnumerable<IFieldable> CreateField(string fieldName, JValue value)
        {
            yield return new Field(fieldName, value.Value<string>(), Field.Store.NO, Field.Index.NOT_ANALYZED);
            yield return new Field(fieldName, value.Value<string>(), Field.Store.NO, Field.Index.ANALYZED);
            yield return new Field("*", value.Value<string>(), Field.Store.NO, Field.Index.ANALYZED);
        }
    }

    public class DefaultIndexStrategy : AbstractIndexStrategy
    {
        public override IEnumerable<IFieldable> CreateField(string fieldName, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Integer:
                    yield return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<long>());
                    break;

                case JTokenType.Float:
                    yield return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetDoubleValue(value.Value<double>());
                    break;
                
                case JTokenType.Date:
                    yield return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<DateTime>().Ticks);
                    break;

                case JTokenType.TimeSpan:
                    yield return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<TimeSpan>().Ticks);
                    break;

                case JTokenType.Guid:
                case JTokenType.Boolean:
                case JTokenType.Raw:
                case JTokenType.Uri:
                    yield return new Field(fieldName, value.Value.ToString(), FieldStore, FieldIndex);
                    break;

                case JTokenType.String:
                    yield return new Field(fieldName, value.Value<string>(), Field.Store.NO, Field.Index.NOT_ANALYZED);
                    yield return new Field(fieldName, value.Value<string>(), Field.Store.NO, Field.Index.ANALYZED);
                    yield return new Field("*", value.Value<string>(), Field.Store.NO, Field.Index.ANALYZED);
                    break;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    yield return new Field(fieldName, "NULL", FieldStore, FieldIndex);
                    break;

                case JTokenType.Bytes:
                    break;
            }
        }
    }

    public class CompoundIndexStrategy : AbstractIndexStrategy
    {
        private readonly Func<JToken, string> compoundFunc;

        public CompoundIndexStrategy(Func<JToken, string> compound)
        {
            compoundFunc = compound;
        }

        public override IEnumerable<IFieldable> CreateField(string fieldName, JValue value)
        {
            string comp = compoundFunc(value);

            //        value.Type:
            //            None,
            //Object,
            //Array,
            //Constructor,
            //Property,
            //Comment,
            //Integer,
            //Float,
            //String,
            //Boolean,
            //Null,
            //Undefined,
            //Date,
            //Raw,
            //Bytes,
            //Guid,
            //Uri,
            //TimeSpan,

            //TODO: Default handling, we should figure out what we might want to do here.
            yield return new Field(fieldName, comp, FieldStore, FieldIndex);
        }
    }
}