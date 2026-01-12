using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class InvidiousApi
{
    // Shared HttpClient for all API calls (best practice - reuse connections)
    private static readonly HttpClient _sharedClient;
    
    static InvidiousApi()
    {
        _sharedClient = new HttpClient();
        _sharedClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/115.0");
        _sharedClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _sharedClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
    }
    
    private HttpClient _client => _sharedClient;

    // Use settings-based URL
    private string BaseUrl => AppSettings.Instance.ApiBaseUrl;

    public InvidiousApi()
    {
        // HttpClient is now shared via static constructor
    }

    public async Task<List<VideoItem>> GetTrendingAsync(string? region = "US")
    {
        try
        {
            var url = $"{BaseUrl}/api/v1/trending?region={region}";
            var response = await _client.GetStringAsync(url);
            var videos = JsonSerializer.Deserialize<List<InvidiousVideo>>(response, JsonOptions);
            return videos?.ConvertAll(v => v.ToVideoItem()) ?? new List<VideoItem>();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error fetching trending: {ex.Message}");
            return new List<VideoItem>();
        }
    }

    public async Task<List<VideoItem>> SearchAsync(string query)
    {
        try
        {
            var encodedQuery = System.Uri.EscapeDataString(query);
            var url = $"{BaseUrl}/api/v1/search?q={encodedQuery}";
            var response = await _client.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<List<InvidiousSearchResult>>(response, JsonOptions);
            
            var videos = new List<VideoItem>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Type == "video")
                    {
                        videos.Add(result.ToVideoItem());
                    }
                }
            }
            return videos;
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error searching: {ex.Message}");
            return new List<VideoItem>();
        }
    }

    public async Task<VideoItem?> GetVideoAsync(string videoId)
    {
        const int maxRetries = 3;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var url = $"{BaseUrl}/api/v1/videos/{videoId}";
                System.Console.WriteLine($"Fetching video details from: {url} (attempt {attempt})");
                
                var response = await _client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    System.Console.WriteLine($"API returned {statusCode}: {response.ReasonPhrase}");
                    
                    // 500/502/503 - server error, worth retrying
                    if (statusCode >= 500 && attempt < maxRetries)
                    {
                        System.Console.WriteLine($"Server error, retrying in {attempt * 2} seconds...");
                        await Task.Delay(attempt * 2000);
                        continue;
                    }
                    
                    return null;
                }
                
                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Response length: {content.Length} chars");
                
                var video = JsonSerializer.Deserialize<InvidiousVideoDetails>(content, JsonOptions);
                
                if (video != null)
                {
                    System.Console.WriteLine($"Parsed video: {video.Title}");
                    System.Console.WriteLine($"FormatStreams count: {video.FormatStreams.Count}");
                    System.Console.WriteLine($"AdaptiveFormats count: {video.AdaptiveFormats.Count}");
                    
                    if (video.FormatStreams.Count > 0)
                    {
                        System.Console.WriteLine($"First format stream URL (truncated): {video.FormatStreams[0].Url?.Substring(0, System.Math.Min(80, video.FormatStreams[0].Url?.Length ?? 0))}...");
                    }
                }
                
                return video?.ToVideoItem();
            }
            catch (HttpRequestException ex)
            {
                System.Console.WriteLine($"HTTP error (attempt {attempt}): {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(attempt * 2000);
                    continue;
                }
            }
            catch (TaskCanceledException ex)
            {
                System.Console.WriteLine($"Timeout (attempt {attempt}): {ex.Message}");
                if (attempt < maxRetries)
                {
                    await Task.Delay(attempt * 1000);
                    continue;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error fetching video: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
        
        return null;
    }

    public string GetThumbnailUrl(string videoId, string quality = "mqdefault")
    {
        return $"{BaseUrl}/vi/{videoId}/{quality}.jpg";
    }

    public string GetVideoStreamUrl(string videoId)
    {
        return $"{BaseUrl}/latest_version?id={videoId}&itag=18";
    }

    public string GetEmbedUrl(string videoId)
    {
        return $"{BaseUrl}/embed/{videoId}";
    }

    public string GetCurrentBaseUrl() => BaseUrl;

    public async Task<ChannelItem?> GetChannelAsync(string channelId)
    {
        try
        {
            var url = $"{BaseUrl}/api/v1/channels/{channelId}";
            Console.WriteLine($"Fetching channel from: {url}");
            var response = await _client.GetStringAsync(url);
            Console.WriteLine($"Channel response length: {response.Length}");
            var channel = JsonSerializer.Deserialize<InvidiousChannel>(response, JsonOptions);
            Console.WriteLine($"Deserialized channel: {channel?.Author}, latest videos: {channel?.LatestVideos?.Count ?? 0}");
            return channel?.ToChannelItem();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error fetching channel {channelId}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CommentItem>> GetCommentsAsync(string videoId, int? continuation = null)
    {
        try
        {
            var url = $"{BaseUrl}/api/v1/comments/{videoId}";
            if (continuation.HasValue)
            {
                url += $"?continuation={continuation}";
            }
            var response = await _client.GetStringAsync(url);
            var commentResponse = JsonSerializer.Deserialize<InvidiousCommentResponse>(response, JsonOptions);
            return commentResponse?.Comments?.ConvertAll(c => c.ToCommentItem()) ?? new List<CommentItem>();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Error fetching comments for {videoId}: {ex.Message}");
            return new List<CommentItem>();
        }
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true
    };
}

// JSON models matching Invidious API responses
public class InvidiousVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public long ViewCount { get; set; }
    public string ViewCountText { get; set; } = "";
    public int LengthSeconds { get; set; }
    public long Published { get; set; }
    public string PublishedText { get; set; } = "";
    public List<InvidiousThumbnail> VideoThumbnails { get; set; } = new();
    public string Description { get; set; } = "";
    public bool IsNew { get; set; }
    public bool Is4k { get; set; }
    public bool LiveNow { get; set; }

    public VideoItem ToVideoItem()
    {
        // Get medium quality thumbnail
        var thumbnail = "";
        foreach (var t in VideoThumbnails)
        {
            if (t.Quality == "medium" || t.Quality == "mqdefault")
            {
                thumbnail = t.Url;
                break;
            }
        }
        if (string.IsNullOrEmpty(thumbnail) && VideoThumbnails.Count > 0)
            thumbnail = VideoThumbnails[0].Url;

        return new VideoItem
        {
            Id = VideoId,
            Title = Title,
            Channel = Author,
            ChannelId = AuthorId,
            Views = string.IsNullOrEmpty(ViewCountText) ? FormatViews(ViewCount) : ViewCountText,
            Duration = FormatDuration(LengthSeconds),
            ThumbnailUrl = thumbnail,
            Description = Description,
            PublishedText = PublishedText,
            IsNew = IsNew,
            Is4k = Is4k,
            IsLive = LiveNow
        };
    }

    private static string FormatViews(long views)
    {
        return views switch
        {
            >= 1_000_000_000 => $"{views / 1_000_000_000.0:F1}B views",
            >= 1_000_000 => $"{views / 1_000_000.0:F1}M views",
            >= 1_000 => $"{views / 1_000.0:F1}K views",
            _ => $"{views} views"
        };
    }

    private static string FormatDuration(int seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0 
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" 
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}

public class InvidiousSearchResult
{
    public string Type { get; set; } = "";
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public bool AuthorVerified { get; set; }
    public long ViewCount { get; set; }
    public string ViewCountText { get; set; } = "";
    public int LengthSeconds { get; set; }
    public string PublishedText { get; set; } = "";
    public List<InvidiousThumbnail> VideoThumbnails { get; set; } = new();
    public string Description { get; set; } = "";
    public bool IsNew { get; set; }
    public bool Is4k { get; set; }
    public bool LiveNow { get; set; }

    public VideoItem ToVideoItem()
    {
        // Get medium quality thumbnail
        var thumbnail = "";
        foreach (var t in VideoThumbnails)
        {
            if (t.Quality == "medium" || t.Quality == "mqdefault")
            {
                thumbnail = t.Url;
                break;
            }
        }
        if (string.IsNullOrEmpty(thumbnail) && VideoThumbnails.Count > 0)
            thumbnail = VideoThumbnails[0].Url;

        return new VideoItem
        {
            Id = VideoId,
            Title = Title,
            Channel = Author,
            ChannelId = AuthorId,
            Views = string.IsNullOrEmpty(ViewCountText) ? FormatViews(ViewCount) : ViewCountText,
            Duration = FormatDuration(LengthSeconds),
            ThumbnailUrl = thumbnail,
            Description = Description,
            PublishedText = PublishedText,
            IsVerified = AuthorVerified,
            IsNew = IsNew,
            Is4k = Is4k,
            IsLive = LiveNow
        };
    }

    private static string FormatViews(long views)
    {
        return views switch
        {
            >= 1_000_000_000 => $"{views / 1_000_000_000.0:F1}B views",
            >= 1_000_000 => $"{views / 1_000_000.0:F1}M views",
            >= 1_000 => $"{views / 1_000.0:F1}K views",
            _ => $"{views} views"
        };
    }

    private static string FormatDuration(int seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0 
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" 
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}

public class InvidiousVideoDetails
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public long ViewCount { get; set; }
    public string ViewCountText { get; set; } = "";
    public int LengthSeconds { get; set; }
    public string PublishedText { get; set; } = "";
    public List<InvidiousThumbnail> VideoThumbnails { get; set; } = new();
    public string Description { get; set; } = "";
    public long LikeCount { get; set; }
    public long DislikeCount { get; set; }
    public List<InvidiousAdaptiveFormat> AdaptiveFormats { get; set; } = new();
    public List<InvidiousFormat> FormatStreams { get; set; } = new();
    public string DashUrl { get; set; } = "";
    public List<InvidiousRecommendedVideo> RecommendedVideos { get; set; } = new();

    public VideoItem ToVideoItem()
    {
        var thumbnail = "";
        foreach (var t in VideoThumbnails)
        {
            if (t.Quality == "medium" || t.Quality == "mqdefault")
            {
                thumbnail = t.Url;
                break;
            }
        }
        if (string.IsNullOrEmpty(thumbnail) && VideoThumbnails.Count > 0)
            thumbnail = VideoThumbnails[0].Url;

        // Get best quality stream URL
        var streamUrl = GetBestStreamUrl();
        
        // Build available quality options
        var qualities = GetAvailableQualities();

        return new VideoItem
        {
            Id = VideoId,
            Title = Title,
            Channel = Author,
            ChannelId = AuthorId,
            Views = string.IsNullOrEmpty(ViewCountText) ? FormatViews(ViewCount) : ViewCountText,
            Duration = FormatDuration(LengthSeconds),
            ThumbnailUrl = thumbnail,
            Description = Description,
            PublishedText = PublishedText,
            LikeCount = LikeCount,
            StreamUrl = streamUrl,
            AudioUrl = GetBestAudioUrl(),
            AvailableQualities = qualities,
            RecommendedVideos = RecommendedVideos.Select(r => r.ToVideoItem()).ToList()
        };
    }
    
    /// <summary>
    /// Get all available quality options sorted by resolution (highest first)
    /// Combined streams (no sync issues) are marked and preferred at same resolution
    /// </summary>
    private List<QualityOption> GetAvailableQualities()
    {
        var qualities = new List<QualityOption>();
        var bestAudioUrl = GetBestAudioUrl();
        
        // FIRST: Add format streams (combined audio+video) - these work best!
        foreach (var stream in FormatStreams)
        {
            var resolution = ParseResolution(stream.QualityLabel);
            if (resolution > 0 && !string.IsNullOrEmpty(stream.Url))
            {
                qualities.Add(new QualityOption
                {
                    Label = $"{stream.QualityLabel} ✓",  // Mark as combined
                    VideoUrl = stream.Url,
                    AudioUrl = null,  // Audio is included - no sync needed!
                    Resolution = resolution,
                    HasSeparateAudio = false,
                    Itag = stream.Itag,
                    Type = stream.Type
                });
            }
        }
        
        // THEN: Add adaptive formats for ALL resolutions not covered by combined streams
        if (AdaptiveFormats.Count > 0)
        {
            var videoStreams = AdaptiveFormats
                .Where(f => f.Type.StartsWith("video/mp4") || f.Type.StartsWith("video/webm"))
                .GroupBy(f => f.QualityLabel)
                .Select(g => g.OrderByDescending(f => ParseBitrate(f.Bitrate)).First())
                .OrderByDescending(f => ParseResolution(f.QualityLabel))
                .ToList();
            
            foreach (var stream in videoStreams)
            {
                var resolution = ParseResolution(stream.QualityLabel);
                // Only add if we don't have this resolution already (prefer combined streams)
                if (resolution > 0 && !qualities.Any(q => q.Resolution == resolution))
                {
                    qualities.Add(new QualityOption
                    {
                        Label = stream.QualityLabel ?? $"{resolution}p",
                        VideoUrl = stream.Url,
                        AudioUrl = bestAudioUrl,
                        Resolution = resolution,
                        HasSeparateAudio = true,
                        Itag = stream.Itag,
                        Type = stream.Type
                    });
                }
            }
        }
        
        // Sort by resolution descending
        qualities = qualities.OrderByDescending(q => q.Resolution).ToList();
        
        System.Console.WriteLine($"Available qualities: {string.Join(", ", qualities.Select(q => q.Label))}");
        
        return qualities;
    }

    private string GetBestStreamUrl()
    {
        // PREFER FormatStreams (combined audio+video) - no sync issues!
        if (FormatStreams.Count > 0)
        {
            // Prefer 720p (itag 22) or 360p (itag 18)
            var best = FormatStreams.FirstOrDefault(f => f.Itag == "22") 
                       ?? FormatStreams.FirstOrDefault(f => f.Itag == "18")
                       ?? FormatStreams[0];
            
            if (!string.IsNullOrEmpty(best.Url))
            {
                System.Console.WriteLine($"Using combined format stream (itag={best.Itag}, quality={best.QualityLabel}) - no audio sync needed");
                return best.Url!;
            }
        }
        
        // Fallback to AdaptiveFormats only if no combined stream available
        if (AdaptiveFormats.Count > 0)
        {
            var videoStreams = AdaptiveFormats
                .Where(f => f.Type.StartsWith("video/mp4") || f.Type.StartsWith("video/webm"))
                .OrderByDescending(f => ParseResolution(f.QualityLabel))
                .ThenByDescending(f => ParseBitrate(f.Bitrate))
                .ToList();
            
            if (videoStreams.Count > 0)
            {
                var best = videoStreams[0];
                System.Console.WriteLine($"Using adaptive format (itag={best.Itag}, quality={best.QualityLabel}, type={best.Type}) - requires audio sync");
                return best.Url;
            }
        }
        
        // Final fallback to Invidious proxy
        var proxyUrl = $"{AppSettings.Instance.ApiBaseUrl}/latest_version?id={VideoId}&itag=22";
        System.Console.WriteLine($"Using proxy URL: {proxyUrl}");
        return proxyUrl;
    }
    
    /// <summary>
    /// Get the best audio stream URL for use with video-only adaptive formats
    /// Uses FormatStream (combined) as the audio source to ensure original language audio
    /// </summary>
    public string? GetBestAudioUrl()
    {
        // PREFER FormatStreams for audio - they always have the original audio track!
        // This avoids picking dubbed audio from AdaptiveFormats
        if (FormatStreams.Count > 0)
        {
            var combined = FormatStreams.FirstOrDefault(f => f.Itag == "22")  // 720p
                        ?? FormatStreams.FirstOrDefault(f => f.Itag == "18")  // 360p
                        ?? FormatStreams[0];
            
            if (!string.IsNullOrEmpty(combined.Url))
            {
                System.Console.WriteLine($"Using FormatStream as audio source (itag={combined.Itag}) - original audio guaranteed");
                return combined.Url;
            }
        }
        
        // Fallback: try to find "original" or default audio in AdaptiveFormats
        // Audio tracks without language suffix are usually the original
        if (AdaptiveFormats.Count > 0)
        {
            var audioStreams = AdaptiveFormats
                .Where(f => f.Type.StartsWith("audio/mp4") || f.Type.StartsWith("audio/webm"))
                .OrderByDescending(f => ParseBitrate(f.Bitrate))
                .ToList();
            
            // Try to find the one that looks like original (usually lower itag numbers are original)
            // Audio itags: 140 (m4a), 251 (opus), 250 (opus), 249 (opus)
            var preferredItags = new[] { "140", "251", "250", "249" };
            foreach (var itag in preferredItags)
            {
                var original = audioStreams.FirstOrDefault(a => a.Itag == itag);
                if (original != null)
                {
                    System.Console.WriteLine($"Using adaptive audio (itag={original.Itag}, bitrate={original.Bitrate})");
                    return original.Url;
                }
            }
            
            // Last resort: just take the first one
            var first = audioStreams.FirstOrDefault();
            if (first != null)
            {
                System.Console.WriteLine($"Using fallback audio (itag={first.Itag}, bitrate={first.Bitrate})");
                return first.Url;
            }
        }
        
        return null;
    }

    private static int ParseResolution(string? qualityLabel)
    {
        if (string.IsNullOrEmpty(qualityLabel)) return 0;
        
        // Extract number from strings like "1080p60", "720p", "2160p", "480p"
        var match = System.Text.RegularExpressions.Regex.Match(qualityLabel, @"(\d+)p");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int res))
            return res;
        
        return 0;
    }

    private static long ParseBitrate(string? bitrate)
    {
        if (string.IsNullOrEmpty(bitrate)) return 0;
        if (long.TryParse(bitrate, out long br)) return br;
        return 0;
    }

    private static string FormatViews(long views)
    {
        return views switch
        {
            >= 1_000_000_000 => $"{views / 1_000_000_000.0:F1}B views",
            >= 1_000_000 => $"{views / 1_000_000.0:F1}M views",
            >= 1_000 => $"{views / 1_000.0:F1}K views",
            _ => $"{views} views"
        };
    }

    private static string FormatDuration(int seconds)
    {
        var ts = System.TimeSpan.FromSeconds(seconds);
        return ts.Hours > 0 
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" 
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }
}

public class InvidiousThumbnail
{
    public string Quality { get; set; } = "";
    public string Url { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
}

public class InvidiousAdaptiveFormat
{
    public string Url { get; set; } = "";
    public string Itag { get; set; } = "";
    public string Type { get; set; } = "";
    public string Container { get; set; } = "";
    public string Encoding { get; set; } = "";
    public string QualityLabel { get; set; } = "";
    public string Bitrate { get; set; } = "";
    public string Resolution { get; set; } = "";
    public int Fps { get; set; }
}

public class InvidiousFormat
{
    public string Url { get; set; } = "";
    public string Itag { get; set; } = "";
    public string Type { get; set; } = "";
    public string Container { get; set; } = "";
    public string QualityLabel { get; set; } = "";
}

public class InvidiousRecommendedVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string AuthorUrl { get; set; } = "";
    public bool AuthorVerified { get; set; }
    public int LengthSeconds { get; set; }
    public long ViewCount { get; set; }
    public string ViewCountText { get; set; } = "";
    public List<InvidiousThumbnail> VideoThumbnails { get; set; } = new();
    
    public VideoItem ToVideoItem()
    {
        var thumbnail = "";
        foreach (var t in VideoThumbnails)
        {
            if (t.Quality == "medium" || t.Quality == "mqdefault")
            {
                thumbnail = t.Url;
                break;
            }
        }
        if (string.IsNullOrEmpty(thumbnail) && VideoThumbnails.Count > 0)
            thumbnail = VideoThumbnails[0].Url;
        
        var channelThumbnail = "";
        // Note: InvidiousVideoDetails doesn't include author thumbnails
        // Channel thumbnails would need to be fetched separately via GetChannelAsync
        
        var ts = System.TimeSpan.FromSeconds(LengthSeconds);
        var duration = ts.Hours > 0 
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" 
            : $"{ts.Minutes}:{ts.Seconds:D2}";
        
        return new VideoItem
        {
            Id = VideoId,
            Title = Title,
            Channel = Author,
            ChannelId = AuthorId,
            ChannelThumbnailUrl = channelThumbnail,
            Views = ViewCountText,
            Duration = duration,
            ThumbnailUrl = thumbnail,
            IsVerified = AuthorVerified
        };
    }
}

public class InvidiousChannel
{
    public string Author { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string AuthorUrl { get; set; } = "";
    public bool AuthorVerified { get; set; }
    public List<InvidiousThumbnail> AuthorBanners { get; set; } = new();
    public List<InvidiousThumbnail> AuthorThumbnails { get; set; } = new();
    public long SubCount { get; set; }
    public long TotalViews { get; set; }
    public long Joined { get; set; }
    public bool AutoGenerated { get; set; }
    public bool IsFamilyFriendly { get; set; }
    public string Description { get; set; } = "";
    public string DescriptionHtml { get; set; } = "";
    public List<string> AllowedRegions { get; set; } = new();
    public List<string> Tabs { get; set; } = new();
    public List<InvidiousVideo> LatestVideos { get; set; } = new();
    public List<InvidiousChannel> RelatedChannels { get; set; } = new();

    public ChannelItem ToChannelItem()
    {
        // Get best thumbnail
        var thumbnailUrl = "";
        if (AuthorThumbnails.Count > 0)
        {
            // Prefer medium quality
            var mediumThumb = AuthorThumbnails.FirstOrDefault(t => t.Quality == "medium");
            if (mediumThumb != null)
            {
                thumbnailUrl = mediumThumb.Url;
            }
            else
            {
                thumbnailUrl = AuthorThumbnails[0].Url;
            }
        }

        // Get banner URL
        var bannerUrl = "";
        if (AuthorBanners.Count > 0)
        {
            bannerUrl = AuthorBanners[0].Url;
        }

        return new ChannelItem
        {
            Id = AuthorId,
            Name = Author,
            Description = Description,
            ThumbnailUrl = thumbnailUrl,
            BannerUrl = bannerUrl,
            SubscriberCount = SubCount,
            VideoCount = LatestVideos.Count, // API doesn't provide video count directly
            LatestVideos = LatestVideos.ConvertAll(v => v.ToVideoItem()),
            Verified = AuthorVerified
        };
    }
}

public class InvidiousComment
{
    public string Author { get; set; } = "";
    public List<InvidiousThumbnail> AuthorThumbnails { get; set; } = new();
    public string AuthorId { get; set; } = "";
    public string AuthorUrl { get; set; } = "";
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
    public bool? IsSponsor { get; set; }
    public string? SponsorIconUrl { get; set; }
    public string Content { get; set; } = "";
    public string ContentHtml { get; set; } = "";
    public long Published { get; set; }
    public string PublishedText { get; set; } = "";
    public int LikeCount { get; set; }
    public string CommentId { get; set; } = "";
    public bool AuthorIsChannelOwner { get; set; }
    public InvidiousCreatorHeart? CreatorHeart { get; set; }
    public InvidiousCommentReplies? Replies { get; set; }

    public CommentItem ToCommentItem()
    {
        // Get best author thumbnail
        var authorThumbnailUrl = "";
        if (AuthorThumbnails.Count > 0)
        {
            // Prefer medium quality
            var mediumThumb = AuthorThumbnails.FirstOrDefault(t => t.Width == 48 || t.Quality == "medium");
            if (mediumThumb != null)
            {
                authorThumbnailUrl = mediumThumb.Url;
            }
            else
            {
                authorThumbnailUrl = AuthorThumbnails[0].Url;
            }
        }

        return new CommentItem
        {
            Id = CommentId,
            Author = Author,
            AuthorId = AuthorId,
            AuthorThumbnailUrl = authorThumbnailUrl,
            Content = Content,
            PublishedText = PublishedText,
            LikeCount = LikeCount,
            IsAuthorChannelOwner = AuthorIsChannelOwner,
            IsPinned = IsPinned,
            Replies = new List<CommentItem>() // For now, we'll handle replies separately if needed
        };
    }
}

public class InvidiousCommentResponse
{
    public int? CommentCount { get; set; }
    public string VideoId { get; set; } = "";
    public List<InvidiousComment> Comments { get; set; } = new();
    public string? Continuation { get; set; }
}

public class InvidiousCreatorHeart
{
    public string CreatorThumbnail { get; set; } = "";
    public string CreatorName { get; set; } = "";
}

public class InvidiousCommentReplies
{
    public int ReplyCount { get; set; }
    public string Continuation { get; set; } = "";
}
