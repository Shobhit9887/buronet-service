using buronet_service.Entities;
using buronet_service.Storage;
using buronet_service.Data;

namespace buronet_service.Services
{
    public class MediaService
    {
        private readonly AppDbContext _db;
        private readonly IBlobStorage _storage;

        public MediaService(AppDbContext db, IBlobStorage storage)
        {
            _db = db;
            _storage = storage;
        }

        public async Task<Guid> UploadAsync(IFormFile file)
        {
            var id = Guid.NewGuid();
            var path = $"media/{id}";

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            await _storage.SaveAsync(path, ms.ToArray());

            _db.MediaFiles.Add(new MediaFile
            {
                Id = id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                StoragePath = path
            });

            await _db.SaveChangesAsync();
            return id;
        }

        public MediaFile Get(Guid id)
            => _db.MediaFiles.Find(id)!;
    }

}
