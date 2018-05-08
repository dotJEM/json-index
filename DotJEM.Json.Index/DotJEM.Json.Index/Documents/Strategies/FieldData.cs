namespace DotJEM.Json.Index.Documents.Strategies
{
    //TODO: Belongs with FieldInformation etc.
    public struct FieldData
    {
        public string Name { get; }
        public string Value { get; }

        public FieldData(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}