namespace AzureHailstone
{
    public interface IUniqueIdGenerator
    {
        long NextId(string scopeName);
    }
}