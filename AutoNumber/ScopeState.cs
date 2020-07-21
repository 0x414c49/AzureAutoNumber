namespace AutoNumber
{
    internal class ScopeState
    {
        public readonly object IdGenerationLock = new object();
        public long HighestIdAvailableInBatch;
        public long LastId;
    }
}