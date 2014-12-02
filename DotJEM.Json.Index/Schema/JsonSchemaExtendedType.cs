using System;

namespace DotJEM.Json.Index.Schema
{
    [Flags]
    public enum JsonSchemaExtendedType
    {
        None = 0,
        String = 1,
        Float = 1 << 1,
        Integer = 1 << 2,
        Boolean = 1 << 3,
        Object = 1 << 4,
        Array = 1 << 5,
        Date = 1 << 6,
        Null = 1 << 7,
        Any = Null | Date | Array | Object | Boolean | Integer | Float | String,
    }
}
