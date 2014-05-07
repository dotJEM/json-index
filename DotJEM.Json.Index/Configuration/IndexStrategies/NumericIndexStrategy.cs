﻿using System;
using System.Globalization;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.IndexStrategies
{
    public class NumericIndexStrategy : AbstractIndexStrategy
    {
        private readonly Func<NumericField, JValue, NumericField> fieldSetter;

        public NumericIndexStrategy(Func<NumericField, JValue, NumericField> setter)
        {
            fieldSetter = setter;
        }

        public override IFieldable CreateField(string fieldName, JValue value)
        {
            if (value.Type != JTokenType.Null && value.Type != JTokenType.Undefined)
            {
                return fieldSetter(new NumericField(fieldName, FieldStore, FieldIndex != Field.Index.NO), value);
            }
            //TODO: Default handling, we should figure out what we might want to do here.
            return new Field(fieldName, value.ToString(CultureInfo.InvariantCulture), FieldStore, FieldIndex);
        }

    }
}