namespace buronet_service.Storage
{
    public class LocalBlobStorage : IBlobStorage
    {
        private readonly string _root;

        public LocalBlobStorage(IWebHostEnvironment env)
        {
            _root = Path.Combine(env.ContentRootPath, "Uploads");
        }

        public async Task SaveAsync(string path, byte[] data)
        {
            var fullPath = Path.Combine(_root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, data);
        }

        public string? GetPath(string path)
            => Path.Combine(_root, path);

        public string? GetPublicUrl(string path)
            => null;
    }
}
