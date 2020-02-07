namespace AzureHailstone.Interfaces
{
    public interface IUniqueIdGenerator
    {
        long NextId(string scopeName);
    }
}