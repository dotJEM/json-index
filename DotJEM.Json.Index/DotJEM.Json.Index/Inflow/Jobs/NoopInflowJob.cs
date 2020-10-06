namespace DotJEM.Json.Index.Inflow.Jobs
{
    public class NoopInflowJob : IInflowJob
    {
        private readonly IReservedSlot slot;
        public int EstimatedCost { get; } = 1;

        public NoopInflowJob(IReservedSlot slot)
        {
            this.slot = slot;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            slot.Ready(null);
        }
    }
}