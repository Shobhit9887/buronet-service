using buronet_service.Data;
using buronet_service.Entities;
using buronet_service.Storage;

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

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            string storagePath;

            if (_storage is CloudinaryBlobStorage cloudinaryStorage)
            {
                // Store returned URL in StoragePath.
                storagePath = await cloudinaryStorage.UploadAsync(
                    ms.ToArray(),
                    file.FileName,
                    file.ContentType,
                    publicId: $"media/{id}");
            }
            else
            {
                // Local fallback
                storagePath = $"media/{id}";
                await _storage.SaveAsync(storagePath, ms.ToArray());
            }

            _db.MediaFiles.Add(new MediaFile
            {
                Id = id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                StoragePath = storagePath
            });

            await _db.SaveChangesAsync();
            return id;
        }

        public MediaFile Get(Guid id)
            => _db.MediaFiles.Find(id)!;
    }
}
