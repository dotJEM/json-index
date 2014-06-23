using System;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IndexStrategies
{
    public class NullIndexStrategy : AbstractIndexStrategy
    {
        public override IFieldable CreateField(string fieldName, JValue value)
        {
            return null;
        }
    }

    public class DefaultIndexStrategy : AbstractIndexStrategy
    {
        public override IFieldable CreateField(string fieldName, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Integer:
                    return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<long>());
                
                case JTokenType.Float:
                    return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetDoubleValue(value.Value<double>());
                
                case JTokenType.Date:
                    return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<DateTime>().Ticks);

                case JTokenType.TimeSpan:
                    return new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO)
                        .SetLongValue(value.Value<TimeSpan>().Ticks);

                case JTokenType.Guid:
                case JTokenType.Boolean:
                case JTokenType.Raw:
                case JTokenType.Uri:
                    return new Field(fieldName, value.Value.ToString(), FieldStore, FieldIndex);

                case JTokenType.String:
                    return new Field(fieldName, value.Value<string>(), FieldStore, FieldIndex);

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return new Field(fieldName, "NULL", FieldStore, FieldIndex);

                case JTokenType.Bytes:
                    break;
            }
            return null;
        }
    }

    public class CompoundIndexStrategy : AbstractIndexStrategy
    {
        private readonly Func<JToken, string> compoundFunc;

        public CompoundIndexStrategy(Func<JToken, string> compound)
        {
            compoundFunc = compound;
        }

        public override IFieldable CreateField(string fieldName, JValue value)
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
            return new Field(fieldName, comp, FieldStore, FieldIndex);
        }
    }
}