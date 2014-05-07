namespace DotJEM.Json.Index.Configuration.IdentityStrategies
{
    public class DefaultIdentityStrategy : FieldIdentityStrategy
    {
        public DefaultIdentityStrategy()
            : base("$id")
        {
        }
    }
}