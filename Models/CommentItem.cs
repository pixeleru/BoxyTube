using System;

/// <summary>
/// Represents a YouTube comment
/// </summary>
public class CommentItem
{
    public string Id { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string AuthorThumbnailUrl { get; set; } = "";
    public string Content { get; set; } = "";
    public string PublishedText { get; set; } = "";
    public long LikeCount { get; set; }
    public bool IsAuthorChannelOwner { get; set; }
    public bool IsPinned { get; set; }
    public List<CommentItem> Replies { get; set; } = new();

    public string LikeCountText => LikeCount > 0 ? $"{LikeCount} likes" : "";
}