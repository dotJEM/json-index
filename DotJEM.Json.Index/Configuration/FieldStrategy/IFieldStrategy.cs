using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.FieldStrategy
{
    public interface IFieldStrategy
    {
        IEnumerable<IFieldable> CreateField(string fieldName, JValue value);
    }

    public abstract class FieldStrategy : IFieldStrategy
    {
        public IEnumerable<IFieldable> CreateField(string fieldName, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Integer:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<long>());
                    break;

                case JTokenType.Float:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetDoubleValue(value.Value<double>());
                    break;

                case JTokenType.Date:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<DateTime>().Ticks);
                    break;

                case JTokenType.TimeSpan:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<TimeSpan>().Ticks);
                    break;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    yield return new Field(fieldName, "NULL", Field.Store.NO, Field.Index.NOT_ANALYZED);
                    yield break;

                case JTokenType.Bytes:
                case JTokenType.None:
                case JTokenType.Object:
                case JTokenType.Array:
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Comment:
                case JTokenType.Raw:
                    yield break;

                //Note: Let these fall thought to the standard tostring handler.
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    break;
            }
            //NOTE: Always add as string.
            string str = value.ToString(CultureInfo.InvariantCulture);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.NOT_ANALYZED);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.ANALYZED);
        }
    } 
}
