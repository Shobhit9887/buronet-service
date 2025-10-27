namespace Buronet.JobService.Settings
{
    public class JobDBSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string JobsCollectionName { get; set; } = null!;
        public string ExamsCollectionName { get; set; } = null!;
        public string ExternalUpdatesCollectionName { get; set; } = null!;
    }
}
