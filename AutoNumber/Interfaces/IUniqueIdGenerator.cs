namespace AutoNumber.Interfaces
{
    public interface IUniqueIdGenerator
    {
        /// <summary>
        /// Generate a new incremental id regards the scope name
        /// </summary>
        /// <param name="scopeName">Generator use this scope name to generate different ids for different scopes</param>
        /// <returns></returns>
        long NextId(string scopeName);
        int BatchSize { get; set; }
        int MaxWriteAttempts { get; set; }
    }
}