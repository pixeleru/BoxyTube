using System.Collections.Generic;

/// <summary>
/// Represents a YouTube channel
/// </summary>
public class ChannelItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string BannerUrl { get; set; } = "";
    public long SubscriberCount { get; set; }
    public long VideoCount { get; set; }
    public List<VideoItem> LatestVideos { get; set; } = new();
    public bool Verified { get; set; }

    public string SubscriberCountText => SubscriberCount >= 1000000
        ? $"{SubscriberCount / 1000000.0:F1}M subscribers"
        : SubscriberCount >= 1000
        ? $"{SubscriberCount / 1000.0:F1}K subscribers"
        : $"{SubscriberCount} subscribers";

    public string VideoCountText => $"{VideoCount} videos";
}