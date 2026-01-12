using System.Collections.Generic;

/// <summary>
/// Represents a quality option for video playback
/// </summary>
public class QualityOption
{
    public string Label { get; set; } = "";      // e.g., "1080p", "720p", "360p"
    public string VideoUrl { get; set; } = "";
    public string? AudioUrl { get; set; }        // Separate audio for adaptive formats
    public int Resolution { get; set; }          // Height in pixels (1080, 720, etc.)
    public bool HasSeparateAudio { get; set; }   // True if audio track is separate
    public string Itag { get; set; } = "";
    public string Type { get; set; } = "";       // video/mp4, video/webm, etc.
    
    public override string ToString() => Label;
}

public class VideoItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Channel { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string ChannelThumbnailUrl { get; set; } = "";
    public string Views { get; set; } = "";
    public string Duration { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string Description { get; set; } = "";
    public string PublishedText { get; set; } = "";
    public long LikeCount { get; set; }
    public string StreamUrl { get; set; } = "";
    public string? AudioUrl { get; set; }  // Separate audio track for adaptive formats
    public List<QualityOption> AvailableQualities { get; set; } = new();  // All available quality options
    public List<VideoItem> RecommendedVideos { get; set; } = new();  // Related/suggested videos
    public bool IsVerified { get; set; }
    public bool IsNew { get; set; }
    public bool Is4k { get; set; }
    public bool IsLive { get; set; }
}
