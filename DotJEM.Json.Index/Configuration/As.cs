using DotJEM.Json.Index.Configuration.FieldStrategies;

namespace DotJEM.Json.Index.Configuration
{
    public static class As
    {
        public static NumericFieldStrategy Integer { get { return new IntegerFieldStragety(); } }
        public static NumericFieldStrategy Long { get { return new LongFieldStragety(); } }
        public static NumericFieldStrategy Float { get { return new FloatFieldStragety(); } }
        public static NumericFieldStrategy Double { get { return new DoubleFieldStragety(); } }
        public static NumericFieldStrategy DateTime { get { return new DateTimeFieldStragety(); } }
        public static NumericFieldStrategy TimeSpan { get { return new TimeSpanFieldStragety(); } }

        public static FieldStrategy Nothing { get { return new NullFieldStrategy(); } }
        public static FieldStrategy Default { get { return new FieldStrategy(); } }
        public static FieldStrategy Analyzed { get {return new FieldStrategy(); }}
        public static FieldStrategy Term { get {return new TermFieldStrategy(); }}
    }

    public static class IndexStrategiesExt
    {
        public static NumericFieldStrategy Integer(this IFieldStrategyBuilder self) { return As.Integer; }
        public static NumericFieldStrategy Long(this IFieldStrategyBuilder self) { return As.Long; }
        public static NumericFieldStrategy Float(this IFieldStrategyBuilder self) { return As.Float; }
        public static NumericFieldStrategy Double(this IFieldStrategyBuilder self) { return As.Double; }
        public static NumericFieldStrategy DateTime(this IFieldStrategyBuilder self) { return As.DateTime; }

        public static FieldStrategy Nothing(this IFieldStrategyBuilder self) { return As.Nothing; }
        public static FieldStrategy Default(this IFieldStrategyBuilder self) { return As.Default; }
        public static FieldStrategy Analyzed(this IFieldStrategyBuilder self) { return As.Analyzed; }
        public static FieldStrategy Term(this IFieldStrategyBuilder self) { return As.Term; }
    }
}