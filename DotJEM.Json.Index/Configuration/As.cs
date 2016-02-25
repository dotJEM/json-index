using DotJEM.Json.Index.Configuration.FieldStrategies;

namespace DotJEM.Json.Index.Configuration
{
    public static class As
    {
        public static NumericFieldStrategy Integer => new NumericFieldStrategy();
        public static NumericFieldStrategy Long => new NumericFieldStrategy();
        public static NumericFieldStrategy Float => new NumericFieldStrategy();
        public static NumericFieldStrategy Double => new NumericFieldStrategy();
        public static NumericFieldStrategy DateTime => new NumericFieldStrategy();
        public static NumericFieldStrategy TimeSpan => new NumericFieldStrategy();

        public static FieldStrategy Nothing => new NullFieldStrategy();
        public static FieldStrategy Default => new FieldStrategy();
        public static FieldStrategy Analyzed => new FieldStrategy();
        public static FieldStrategy Term => new TermFieldStrategy();
    }

    public static class IndexStrategiesExt
    {
        public static NumericFieldStrategy Integer(this IFieldStrategyBuilder self) => As.Integer;
        public static NumericFieldStrategy Long(this IFieldStrategyBuilder self) => As.Long;
        public static NumericFieldStrategy Float(this IFieldStrategyBuilder self) => As.Float;
        public static NumericFieldStrategy Double(this IFieldStrategyBuilder self) => As.Double;
        public static NumericFieldStrategy DateTime(this IFieldStrategyBuilder self) => As.DateTime;

        public static FieldStrategy Nothing(this IFieldStrategyBuilder self) => As.Nothing;
        public static FieldStrategy Default(this IFieldStrategyBuilder self) => As.Default;
        public static FieldStrategy Analyzed(this IFieldStrategyBuilder self) => As.Analyzed;
        public static FieldStrategy Term(this IFieldStrategyBuilder self) => As.Term;
    }
}