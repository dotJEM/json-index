using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public interface IJTokenVisitor<in TContext>
    {
        void Visit(JToken json, TContext context);
    }
}