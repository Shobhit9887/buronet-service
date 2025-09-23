// File: Buronet.JobService/Models/ApplyLinkInfo.cs
using MongoDB.Bson.Serialization.Attributes;

namespace Buronet.JobService.Models;

public class ApplyLinkInfo
{
    [BsonElement("link")]
    public string Link { get; set; } = string.Empty;

    [BsonElement("file_name")]
    public string FileName { get; set; } = string.Empty;
}