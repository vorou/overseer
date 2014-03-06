namespace Overseer.WebApp
{
    public interface IRegionNameService
    {
        void Fetch();
        string GetName(string id);
    }
}