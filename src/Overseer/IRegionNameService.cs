namespace Overseer
{
    public interface IRegionNameService
    {
        void Fetch();
        string GetName(string id);
    }
}