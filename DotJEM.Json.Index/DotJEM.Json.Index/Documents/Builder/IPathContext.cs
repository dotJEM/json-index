using DotJEM.Json.Visitor;

namespace DotJEM.Json.Index.Documents.Builder
{

    public interface IPathContext : IJsonVisitorContext<IPathContext>
    {
        string Path { get; }
    }
}