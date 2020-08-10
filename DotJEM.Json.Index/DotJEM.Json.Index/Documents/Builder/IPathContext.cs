using DotJEM.Json.Visitor;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{

    public interface IPathContext : IJsonVisitorContext<IPathContext>
    {
        string Path { get; }
    }
}