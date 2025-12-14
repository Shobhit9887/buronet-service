namespace buronet_service.Entities
{
    public class MediaFile
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public string StoragePath { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
