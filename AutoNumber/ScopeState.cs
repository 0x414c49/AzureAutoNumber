namespace AutoNumber
{
    internal class ScopeState
    {
        public readonly object IdGenerationLock = new();
        public long HighestIdAvailableInBatch;
        public long LastId;
    }
}