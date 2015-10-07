namespace DotJEM.Json.Index.Sharding.Resolvers
{
    public interface IServiceResolver
    {
        TService Resolve<TService>();
    }
}