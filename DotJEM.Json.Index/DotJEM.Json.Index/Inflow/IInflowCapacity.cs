namespace DotJEM.Json.Index.Inflow
{
    public interface IInflowCapacity
    {
        void Free(int estimatedCost);
        void Allocate(int estimatedCost);
    }

    public class NullInflowCapacity : IInflowCapacity
    {
        public void Free(int estimatedCost)
        {
        }

        public void Allocate(int estimatedCost)
        {
        }
    }
}