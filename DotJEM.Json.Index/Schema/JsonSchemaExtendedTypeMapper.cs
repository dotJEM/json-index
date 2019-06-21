using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    public static class JsonSchemaExtendedTypeMapper
    {
        private static readonly IDictionary<JTokenType, JsonSchemaExtendedType> map;

        static JsonSchemaExtendedTypeMapper()
        {
            map = new Dictionary<JTokenType, JsonSchemaExtendedType>();
            map[JTokenType.None] = JsonSchemaExtendedType.None;
            map[JTokenType.Object] = JsonSchemaExtendedType.Object;
            map[JTokenType.Array] = JsonSchemaExtendedType.Array;
            map[JTokenType.Integer] = JsonSchemaExtendedType.Integer;
            map[JTokenType.Float] = JsonSchemaExtendedType.Float;
            map[JTokenType.String] = JsonSchemaExtendedType.String;
            map[JTokenType.Boolean] = JsonSchemaExtendedType.Boolean;
            map[JTokenType.Date] = JsonSchemaExtendedType.Date;
            map[JTokenType.Null] = JsonSchemaExtendedType.Null;

            //NOTE: Unsupported types, perhaps these should be any instead?
            map[JTokenType.Raw] = JsonSchemaExtendedType.Any;
            map[JTokenType.Bytes] = JsonSchemaExtendedType.Any;
            map[JTokenType.Guid] = JsonSchemaExtendedType.Guid;
            map[JTokenType.Uri] = JsonSchemaExtendedType.Uri;
            map[JTokenType.TimeSpan] = JsonSchemaExtendedType.TimeSpan;
            map[JTokenType.Undefined] = JsonSchemaExtendedType.Null;
        }

        public static JsonSchemaExtendedType ToSchemaExtendedType(this JTokenType self)
        {
            return map[self];
        }
    }
}