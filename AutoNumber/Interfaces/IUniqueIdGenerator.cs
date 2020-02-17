namespace AutoNumber.Interfaces
{
    public interface IUniqueIdGenerator
    {
        long NextId(string scopeName);
    }
}