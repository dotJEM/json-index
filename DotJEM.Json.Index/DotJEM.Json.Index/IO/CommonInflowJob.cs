using DotJEM.Json.Index.Inflow;

namespace DotJEM.Json.Index.IO
{
    public class CommonInflowJob : IInflowJob
    {
        private readonly IReservedSlot slot;
        public int EstimatedCost { get; } = 1;

        public CommonInflowJob(IReservedSlot slot)
        {
            this.slot = slot;
        }
        public void Execute(IInflowScheduler scheduler)
        {
            slot.Complete();
        }
    }
}