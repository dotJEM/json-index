namespace DotJEM.Json.Index.Configuration.IdentityStrategies
{
    public class DefaultIdentityResolver : FieldIdentityResolver
    {
        public DefaultIdentityResolver()
            : base("$id")
        {
        }
    }
}