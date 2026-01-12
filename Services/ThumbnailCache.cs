using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Gdk;

/// <summary>
/// Shared thumbnail loader with caching to avoid re-downloading the same images.
/// Uses a single HttpClient for connection pooling.
/// </summary>
public static class ThumbnailCache
{
    private static readonly HttpClient _client;
    private static readonly ConcurrentDictionary<string, Pixbuf?> _cache = new();
    private const int MaxCacheSize = 200;
    
    static ThumbnailCache()
    {
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(5);
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }
    
    /// <summary>
    /// Load a thumbnail from YouTube CDN with caching
    /// </summary>
    public static async Task<Pixbuf?> LoadAsync(string videoId, int width, int height)
    {
        var cacheKey = $"{videoId}_{width}x{height}";
        
        // Check cache first
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }
        
        try
        {
            var url = $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg";
            var imageData = await _client.GetByteArrayAsync(url);
            
            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Pixbuf(stream);
            var scaled = pixbuf.ScaleSimple(width, height, InterpType.Bilinear);
            
            // Add to cache (simple eviction if too large)
            if (_cache.Count >= MaxCacheSize)
            {
                // Clear half the cache when full
                var keys = _cache.Keys.Take(MaxCacheSize / 2).ToArray();
                foreach (var key in keys)
                {
                    _cache.TryRemove(key, out _);
                }
            }
            
            _cache[cacheKey] = scaled;
            return scaled;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Thumbnail load failed for {videoId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Clear the thumbnail cache
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
    }
}
