namespace buronet_service.Storage
{
    public interface IBlobStorage
    {
        Task SaveAsync(string path, byte[] data);
        string GetPath(string path);
    }

}
