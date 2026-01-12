namespace MyFirstGtkApp.Components
{
using System;
using System.Runtime.InteropServices;
using Gtk;

/// <summary>
/// A video player component that uses WebKitGTK to play videos via HTML5.
/// This allows playing raw Google video URLs that require specific headers.
/// </summary>
public class WebKitVideoPlayer : Box
{
    private Widget? _webView;
    private IntPtr _webViewPtr = IntPtr.Zero;
    private Label _fallbackLabel;
    private bool _isWebKitAvailable;

    // WebKitGTK bindings
    private const string WebKitLib = "libwebkit2gtk-4.1.so.0";
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr webkit_web_view_new();
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_web_view_load_html(IntPtr webView, string content, string? baseUri);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_web_view_load_uri(IntPtr webView, string uri);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr webkit_web_view_get_settings(IntPtr webView);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_enable_media_stream(IntPtr settings, bool enabled);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_enable_webaudio(IntPtr settings, bool enabled);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_media_playback_requires_user_gesture(IntPtr settings, bool required);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_enable_fullscreen(IntPtr settings, bool enabled);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_allow_file_access_from_file_urls(IntPtr settings, bool allowed);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_enable_media(IntPtr settings, bool enabled);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_web_view_run_javascript(IntPtr webView, string script, IntPtr cancellable, IntPtr callback, IntPtr userData);

    // Hardware acceleration settings
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_hardware_acceleration_policy(IntPtr settings, int policy);
    
    [DllImport(WebKitLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern void webkit_settings_set_enable_accelerated_2d_canvas(IntPtr settings, bool enabled);
    
    // Hardware acceleration policy enum values
    private const int WEBKIT_HARDWARE_ACCELERATION_POLICY_ON_DEMAND = 0;
    private const int WEBKIT_HARDWARE_ACCELERATION_POLICY_ALWAYS = 1;
    private const int WEBKIT_HARDWARE_ACCELERATION_POLICY_NEVER = 2;

    // GObject signal connection for fullscreen handling
    private const string GObjectLib = "libgobject-2.0.so.0";
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool FullscreenCallback(IntPtr webView, IntPtr userData);

    [DllImport(GObjectLib, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong g_signal_connect_data(IntPtr instance, string signal, Delegate handler, IntPtr data, IntPtr destroyData, int flags);

    // Store delegates to prevent garbage collection
    private FullscreenCallback? _enterFullscreenCallback;
    private FullscreenCallback? _leaveFullscreenCallback;
    private Gtk.Window? _fullscreenWindow;

    public WebKitVideoPlayer() : base(Orientation.Vertical, 0)
    {
        // Make this container fill available space
        Hexpand = true;
        Vexpand = true;
        Halign = Align.Fill;
        Valign = Align.Fill;
        
        _fallbackLabel = new Label("WebKit not available")
        {
            Halign = Align.Center,
            Valign = Align.Center
        };

        try
        {
            InitializeWebKit();
            _isWebKitAvailable = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebKit initialization failed: {ex.Message}");
            _isWebKitAvailable = false;
            _fallbackLabel.Text = $"WebKit not available: {ex.Message}";
            PackStart(_fallbackLabel, true, true, 0);
        }
    }

    private void InitializeWebKit()
    {
        // Create the WebView
        _webViewPtr = webkit_web_view_new();
        
        if (_webViewPtr == IntPtr.Zero)
        {
            throw new Exception("Failed to create WebKit WebView");
        }

        // Configure settings for video playback and fullscreen
        var settings = webkit_web_view_get_settings(_webViewPtr);
        if (settings != IntPtr.Zero)
        {
            webkit_settings_set_enable_media_stream(settings, true);
            webkit_settings_set_enable_webaudio(settings, true);
            webkit_settings_set_media_playback_requires_user_gesture(settings, false);
            webkit_settings_set_enable_fullscreen(settings, true);
            webkit_settings_set_allow_file_access_from_file_urls(settings, true);
            
            // Enable hardware acceleration (VAAPI/VA-API support)
            // This tells WebKit to use GPU for video decoding when available
            webkit_settings_set_hardware_acceleration_policy(settings, WEBKIT_HARDWARE_ACCELERATION_POLICY_ALWAYS);
            webkit_settings_set_enable_accelerated_2d_canvas(settings, true);
            Console.WriteLine("Hardware acceleration enabled (VAAPI)");
        }

        // Wrap the native pointer in a GTK Widget
        _webView = new Widget(_webViewPtr);
        _webView.Hexpand = true;
        _webView.Vexpand = true;
        _webView.Valign = Align.Fill;
        _webView.Halign = Align.Fill;
        _webView.Show();

        // Connect fullscreen signals for native video fullscreen
        _enterFullscreenCallback = OnEnterFullscreen;
        _leaveFullscreenCallback = OnLeaveFullscreen;
        g_signal_connect_data(_webViewPtr, "enter-fullscreen", _enterFullscreenCallback, IntPtr.Zero, IntPtr.Zero, 0);
        g_signal_connect_data(_webViewPtr, "leave-fullscreen", _leaveFullscreenCallback, IntPtr.Zero, IntPtr.Zero, 0);

        PackStart(_webView, true, true, 0);
        
        Console.WriteLine("WebKit WebView initialized successfully");
    }

    private bool OnEnterFullscreen(IntPtr webView, IntPtr userData)
    {
        Console.WriteLine("Video entering fullscreen");
        
        GLib.Idle.Add(() =>
        {
            // Get the toplevel window and fullscreen it
            var toplevel = Toplevel as Gtk.Window;
            if (toplevel != null)
            {
                _fullscreenWindow = toplevel;
                toplevel.Fullscreen();
            }
            return false;
        });
        
        return false;
    }

    private bool OnLeaveFullscreen(IntPtr webView, IntPtr userData)
    {
        Console.WriteLine("Video leaving fullscreen");
        
        GLib.Idle.Add(() =>
        {
            if (_fullscreenWindow != null)
            {
                _fullscreenWindow.Unfullscreen();
                _fullscreenWindow = null;
            }
            return false;
        });
        
        return false;
    }

    /// <summary>
    /// Load and play a video from a URL using native HTML5 video
    /// </summary>
    public void PlayVideo(string videoUrl, string? audioUrl = null, string? title = null)
    {
        if (!_isWebKitAvailable || _webViewPtr == IntPtr.Zero)
        {
            Console.WriteLine("WebKit not available for video playback");
            return;
        }

        bool hasSeparateAudio = !string.IsNullOrEmpty(audioUrl);
        Console.WriteLine($"Playing video URL: {videoUrl}");
        Console.WriteLine($"Separate audio: {(hasSeparateAudio ? audioUrl : "No (combined stream)")}");

        // Centralized JS helper for both single and dual video
        string html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<style>
*{{margin:0;padding:0;box-sizing:border-box}}
html,body{{width:100%;height:100%;background:#000;overflow:hidden}}
#mainVideo, #video{{position:absolute;top:0;left:0;width:100%;height:100%;object-fit:contain;background:#000}}
#audioVideo{{position:absolute;top:0;left:0;width:1px;height:1px;opacity:0;pointer-events:none}}
video::-webkit-media-controls-fullscreen-button{{display:none !important}}
.loading,.error{{position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);color:#fff;font:18px sans-serif;text-align:center}}
.error{{color:#f66}}
</style>
</head>
<body>
    {(hasSeparateAudio ? "<video id='mainVideo' playsinline muted></video><video id='audioVideo' playsinline></video>" : "<video id='video' controls playsinline></video>")}
    <div id='loading' class='loading'>Loading...</div>
    <div id='error' class='error' style='display:none'></div>
<script>
window.VideoSync = (function() {{
    var hasSeparateAudio = {hasSeparateAudio.ToString().ToLowerInvariant()};
    var mainVideo = document.getElementById(hasSeparateAudio ? 'mainVideo' : 'video');
    var audioVideo = hasSeparateAudio ? document.getElementById('audioVideo') : null;
    var loading = document.getElementById('loading');
    var errorDiv = document.getElementById('error');
    var isReady = {{ video: false, audio: !hasSeparateAudio }};
    var hasError = false;
    var isSyncing = false;
    var hasStarted = false;
    var syncInterval = null;

    function showError(msg) {{
        if (hasError) return;
        hasError = true;
        loading.style.display = 'none';
        errorDiv.textContent = msg;
        errorDiv.style.display = 'block';
        if (syncInterval) clearInterval(syncInterval);
    }}

    // Error handlers
    mainVideo.addEventListener('error', function() {{
        showError('Video failed to load');
        console.log('Video error:', mainVideo.error);
    }});
    if (audioVideo) {{
        audioVideo.addEventListener('error', function() {{
            showError('Audio failed to load');
            console.log('Audio error:', audioVideo.error);
        }});
    }}

    // Sources
    mainVideo.src = '{videoUrl.Replace("'", "\\'")}';
    if (audioVideo) audioVideo.src = '{(audioUrl ?? string.Empty).Replace("'", "\\'")}';

    function checkReady() {{
        if (hasError || hasStarted) return;
        if (isReady.video && isReady.audio) {{
            hasStarted = true;
            loading.style.display = 'none';
            startSync();
        }}
    }}

    function startSync() {{
        mainVideo.currentTime = 0;
        if (audioVideo) audioVideo.currentTime = 0;
        mainVideo.muted = true;
        if (audioVideo) audioVideo.muted = true;
        var playPromises = [mainVideo.play().catch(function(e){{console.log('Video play failed:',e);}})];
        if (audioVideo) playPromises.push(audioVideo.play().catch(function(e){{console.log('Audio play failed:',e);}}));
        Promise.all(playPromises).then(function() {{
            mainVideo.controls = true;
            setTimeout(function() {{
                mainVideo.muted = false;
                if (audioVideo) audioVideo.muted = false;
            }}, 100);
        }});
    }}

    // Wait for both to be ready (once only)
    mainVideo.addEventListener('canplaythrough', function() {{
        if (!isReady.video) {{ isReady.video = true; checkReady(); }}
    }}, {{once: true}});
    if (audioVideo) {{
        audioVideo.addEventListener('canplaythrough', function() {{
            if (!isReady.audio) {{ isReady.audio = true; checkReady(); }}
        }}, {{once: true}});
    }}

    // Event syncing
    mainVideo.addEventListener('play', function() {{
        if (hasError) return;
        if (audioVideo) {{
            audioVideo.currentTime = mainVideo.currentTime;
            audioVideo.play().catch(function(){{}});
        }}
    }});
    mainVideo.addEventListener('pause', function() {{
        if (audioVideo) audioVideo.pause();
    }});
    mainVideo.addEventListener('seeking', function() {{
        isSyncing = true;
        if (audioVideo) audioVideo.currentTime = mainVideo.currentTime;
    }});
    mainVideo.addEventListener('seeked', function() {{
        if (audioVideo) {{
            audioVideo.currentTime = mainVideo.currentTime;
            if (!mainVideo.paused) audioVideo.play().catch(function(){{}});
        }}
        isSyncing = false;
    }});
    mainVideo.addEventListener('volumechange', function() {{
        if (audioVideo) audioVideo.volume = mainVideo.muted ? 0 : mainVideo.volume;
    }});
    mainVideo.addEventListener('ratechange', function() {{
        if (audioVideo) audioVideo.playbackRate = mainVideo.playbackRate;
    }});

    // Continuous sync correction (every 500ms, with 150ms tolerance)
    syncInterval = setInterval(function() {{
        if (hasError) {{ clearInterval(syncInterval); return; }}
        if (!mainVideo.paused && !isSyncing && !mainVideo.ended && audioVideo) {{
            var drift = mainVideo.currentTime - audioVideo.currentTime;
            if (Math.abs(drift) > 0.15) {{
                audioVideo.currentTime = mainVideo.currentTime;
            }}
        }}
    }}, 500);

    // Preload
    mainVideo.load();
    if (audioVideo) audioVideo.load();

    // Public API
    return {{
        resume: function() {{
            if (hasError) return;
            if (audioVideo) {{
                audioVideo.currentTime = mainVideo.currentTime;
                audioVideo.muted = mainVideo.muted;
                audioVideo.volume = mainVideo.volume;
                if (!mainVideo.paused) audioVideo.play().catch(function(){{}});
            }}
            mainVideo.muted = false;
            mainVideo.play().catch(function(e){{console.log('Resume failed:',e);}});
        }}
    }};
}})();
</script>
</body>
</html>";

        webkit_web_view_load_html(_webViewPtr, html, AppSettings.Instance.ApiBaseUrl);
    }

    /// <summary>
    /// Load and play a YouTube video using the embed URL
    /// </summary>
    public void PlayYouTubeEmbed(string videoId)
    {
        if (!_isWebKitAvailable || _webViewPtr == IntPtr.Zero)
        {
            Console.WriteLine("WebKit not available for video playback");
            return;
        }

        var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        * { margin: 0; padding: 0; }
        html, body { width: 100%; height: 100%; background: #000; }
        iframe { width: 100%; height: 100%; border: none; }
    </style>
</head>
<body>
    <iframe 
        src='https://www.youtube.com/embed/" + videoId + @"?autoplay=1&rel=0' 
        allow='autoplay; encrypted-media; fullscreen'
        allowfullscreen>
    </iframe>
</body>
</html>";

        Console.WriteLine($"Loading YouTube embed: {videoId}");
        webkit_web_view_load_html(_webViewPtr, html, null);
    }

    /// <summary>
    /// Load a URL directly in the WebView
    /// </summary>
    public void LoadUri(string uri)
    {
        if (!_isWebKitAvailable || _webViewPtr == IntPtr.Zero) return;
        webkit_web_view_load_uri(_webViewPtr, uri);
    }

    /// <summary>
    /// Show a loading/placeholder message
    /// </summary>
    public void ShowMessage(string message)
    {
        if (!_isWebKitAvailable || _webViewPtr == IntPtr.Zero) return;
        
        var html = @"<!DOCTYPE html>
<html>
<head>
    <style>
        body { 
            background: #1a1a1a; 
            color: #fff; 
            font-family: sans-serif;
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100vh;
            margin: 0;
        }
        .message { text-align: center; opacity: 0.7; }
    </style>
</head>
<body>
    <div class='message'><h2>" + message + @"</h2></div>
</body>
</html>";
        
        webkit_web_view_load_html(_webViewPtr, html, null);
    }
    
    /// <summary>
    /// Resume playback after reparenting (e.g., fullscreen toggle).
    /// This re-syncs the audio with video and ensures playback continues.
    /// </summary>
    public void ResumePlayback()
    {
        if (!_isWebKitAvailable || _webViewPtr == IntPtr.Zero) return;
        Console.WriteLine("Resuming playback after reparent");
        // Call the centralized JS helper to resume and sync playback
        var script = "if(window.VideoSync && typeof window.VideoSync.resume==='function'){window.VideoSync.resume();}";
        webkit_web_view_run_javascript(_webViewPtr, script, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    public bool IsAvailable => _isWebKitAvailable;
}
}