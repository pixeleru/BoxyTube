using Gtk;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MyFirstGtkApp.Components;

public class VideoPlayerView : Box
{
    private readonly MainWindow _mainWindow;
    private readonly InvidiousApi _api;
    private VideoItem? _currentVideo;
    private VideoItem? _currentVideoDetails;
    
    // UI Elements
    private Label _videoTitle = null!;
    private Label _videoChannel = null!;
    private Image _channelImage = null!;
    private Label _videoDescription = null!;
    private ComboBoxText _qualityCombo = null!;
    private Image _thumbnailImage = null!;
    private Box _thumbnailBox = null!;
    private Box _recommendedList = null!;
    private Label _recommendedLabel = null!;
    private Notebook _notebook = null!;
    private CommentsView? _commentsView;
    private Box _controlsBar = null!;
    private Separator _separator = null!;
    private ScrolledWindow _sidebarScroll = null!;
    
    // WebKit embedded player
    private WebKitVideoPlayer? _webKitPlayer;
    private Box _playerContainer = null!;
    private bool _isEmbedPlaying;
    
    // Fullscreen
    private Gtk.Window? _fullscreenWindow;
    private bool _isFullscreen;

    public bool IsPlaying => _isEmbedPlaying;
    
    public event EventHandler? PlaybackStopped;

    public void SetBackgroundMode(bool isBackground)
    {
        if (isBackground)
        {
            _separator.Hide();
            _sidebarScroll.Hide();
        }
        else
        {
            _separator.Show();
            _sidebarScroll.Show();
        }
    }

    public VideoPlayerView(MainWindow mainWindow) : base(Orientation.Vertical, 0)
    {
        _mainWindow = mainWindow;
        _api = new InvidiousApi();
        BuildUI();
    }
    
    /// <summary>
    /// Stop video playback (called from MainWindow when navigating away)
    /// </summary>
    public void StopPlayback()
    {
        StopEmbedPlayback();
    }

    private void BuildUI()
    {
        // Main content: Horizontal layout (player left, info+suggestions right)
        var mainContent = new Box(Orientation.Horizontal, 0);

        // ═══════════════════════════════════════════════════════════
        // LEFT SIDE: Video player + controls only
        // ═══════════════════════════════════════════════════════════
        var leftPane = new Box(Orientation.Vertical, 0)
        {
            Hexpand = true,
            Vexpand = true
        };

        // Player container
        _playerContainer = new Box(Orientation.Vertical, 0)
        {
            Hexpand = true,
            Vexpand = true
        };
        _playerContainer.StyleContext.AddClass("view");

        // Thumbnail placeholder
        _thumbnailBox = new Box(Orientation.Vertical, 0)
        {
            Halign = Align.Fill,
            Valign = Align.Fill
        };
        _thumbnailImage = new Image();
        _thumbnailBox.PackStart(_thumbnailImage, true, true, 0);
        _playerContainer.PackStart(_thumbnailBox, true, true, 0);

        // WebKit player
        try
        {
            _webKitPlayer = new WebKitVideoPlayer();
            _webKitPlayer.NoShowAll = true;
            _webKitPlayer.Hide();
            _webKitPlayer.Vexpand = true;
            _webKitPlayer.Hexpand = true;
            _playerContainer.PackStart(_webKitPlayer, true, true, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebKit init failed: {ex.Message}");
            _webKitPlayer = null;
        }

        leftPane.PackStart(_playerContainer, true, true, 0);

        // Controls bar below player
        _controlsBar = new Box(Orientation.Horizontal, 8)
        {
            MarginStart = 12,
            MarginEnd = 12,
            MarginTop = 8,
            MarginBottom = 8
        };

        // Quality selector
        _qualityCombo = new ComboBoxText();
        _qualityCombo.Changed += OnQualityChanged;
        _controlsBar.PackStart(_qualityCombo, false, false, 0);


        // Fullscreen button
        var fullscreenBtn = new Button("⛶");
        fullscreenBtn.TooltipText = "Fullscreen (F)";
        fullscreenBtn.Clicked += (s, e) => ToggleFullscreen();
        _controlsBar.PackStart(fullscreenBtn, false, false, 0);

        // Spacer
        _controlsBar.PackStart(new Box(Orientation.Horizontal, 0) { Hexpand = true }, true, true, 0);

        // Browser button
        var browserBtn = new Button("🌐");
        browserBtn.TooltipText = "Open in browser";
        browserBtn.Clicked += (s, e) => OpenUrl($"{AppSettings.Instance.ApiBaseUrl}/watch?v={_currentVideo?.Id}");
        _controlsBar.PackEnd(browserBtn, false, false, 0);

        leftPane.PackStart(_controlsBar, false, false, 0);

        mainContent.PackStart(leftPane, true, true, 0);
        _separator = new Separator(Orientation.Vertical);
        mainContent.PackStart(_separator, false, false, 0);

        // ═══════════════════════════════════════════════════════════
        // RIGHT SIDE: Tabs for Video info and Comments
        // ═══════════════════════════════════════════════════════════
        var rightPane = new Box(Orientation.Vertical, 0)
        {
            WidthRequest = 260
        };
        rightPane.StyleContext.AddClass("sidebar");

        // Create notebook for tabs
        _notebook = new Notebook();
        _notebook.TabPos = PositionType.Top;

        // Info tab
        var infoTab = new Box(Orientation.Vertical, 0);
        var infoLabel = new Label("Info");

        // Video info section
        var infoBox = new Box(Orientation.Vertical, 4)
        {
            MarginTop = 12,
            MarginStart = 12,
            MarginEnd = 12,
            MarginBottom = 12
        };

        _videoTitle = new Label("")
        {
            Xalign = 0,
            Wrap = true,
            MaxWidthChars = 40
        };
        _videoTitle.StyleContext.AddClass("title-3");

        // Channel info with image and name
        var channelBox = new Box(Orientation.Horizontal, 8);

        _channelImage = new Image
        {
            WidthRequest = 32,
            HeightRequest = 32
        };

        _videoChannel = new Label("")
        {
            Xalign = 0,
            Opacity = 0.7
        };
        _videoChannel.StyleContext.AddClass("dim-label");

        // Make channel clickable
        var channelEventBox = new EventBox();
        channelEventBox.Add(channelBox);
        channelEventBox.ButtonPressEvent += OnChannelClicked;

        channelBox.PackStart(_channelImage, false, false, 0);
        channelBox.PackStart(_videoChannel, false, false, 0);

        _videoDescription = new Label("")
        {
            Xalign = 0,
            Wrap = true,
            MaxWidthChars = 45,
            Selectable = true,
            Opacity = 0.9
        };
        // Make description scrollable and limit height
        var descScroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            HeightRequest = 160,
            ShadowType = ShadowType.In
        };
        descScroll.Add(_videoDescription);

        infoBox.PackStart(_videoTitle, false, false, 0);
        infoBox.PackStart(channelEventBox, false, false, 0);
        infoBox.PackStart(new Separator(Orientation.Horizontal) { MarginTop = 8, MarginBottom = 8 }, false, false, 0);
        infoBox.PackStart(descScroll, false, false, 0);

        // Suggested videos section
        _recommendedLabel = new Label("Suggested Videos")
        {
            Xalign = 0,
            MarginStart = 12,
            MarginTop = 12,
            MarginBottom = 8
        };
        _recommendedLabel.StyleContext.AddClass("title-4");

        var recommendedScroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            Vexpand = true
        };

        _recommendedList = new Box(Orientation.Vertical, 4)
        {
            MarginStart = 8,
            MarginEnd = 8,
            MarginBottom = 8
        };

        recommendedScroll.Add(_recommendedList);

        infoTab.PackStart(infoBox, false, false, 0);
        infoTab.PackStart(new Separator(Orientation.Horizontal), false, false, 0);
        infoTab.PackStart(_recommendedLabel, false, false, 0);
        infoTab.PackStart(recommendedScroll, true, true, 0);

        _notebook.AppendPage(infoTab, infoLabel);

        // Comments tab
        var commentsTab = new Box(Orientation.Vertical, 0);
        var commentsLabel = new Label("Comments");
        // CommentsView will be added when video loads
        _notebook.AppendPage(commentsTab, commentsLabel);

        rightPane.PackStart(_notebook, true, true, 0);

        // Make sidebar scrollable if content overflows
        _sidebarScroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            Hexpand = false,
            Vexpand = true
        };
        _sidebarScroll.Add(rightPane);
        mainContent.PackStart(_sidebarScroll, false, false, 0);

        PackStart(mainContent, true, true, 0);
    }

    private void OnQualityChanged(object? sender, EventArgs e)
    {
        if (!_isEmbedPlaying || _currentVideoDetails == null || _webKitPlayer == null) return;

        var activeId = _qualityCombo.ActiveId;
        if (string.IsNullOrEmpty(activeId)) return;

        if (int.TryParse(activeId, out int index) && index >= 0 && index < _currentVideoDetails.AvailableQualities.Count)
        {
            var q = _currentVideoDetails.AvailableQualities[index];
            Console.WriteLine($"Switching to: {q.Label}");
            _webKitPlayer.PlayVideo(q.VideoUrl, q.AudioUrl, _currentVideo?.Title);
        }
    }

    private void ToggleFullscreen()
    {
        if (_webKitPlayer == null || !_isEmbedPlaying) return;

        if (_isFullscreen)
            ExitFullscreen();
        else
            EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        if (_isFullscreen || _webKitPlayer == null) return;

        _fullscreenWindow = new Gtk.Window(WindowType.Toplevel)
        {
            Title = "Fullscreen",
            Decorated = false
        };
        _fullscreenWindow.SetDefaultSize(1920, 1080);

        _playerContainer.Remove(_webKitPlayer);
        _fullscreenWindow.Add(_webKitPlayer);

        _fullscreenWindow.KeyPressEvent += (s, e) =>
        {
            if (e.Event.Key == Gdk.Key.Escape || e.Event.Key == Gdk.Key.f)
            {
                ExitFullscreen();
                e.RetVal = true;
            }
        };
        _fullscreenWindow.DeleteEvent += (s, e) => { ExitFullscreen(); e.RetVal = true; };

        _fullscreenWindow.ShowAll();
        _fullscreenWindow.Fullscreen();
        _fullscreenWindow.Present();
        _isFullscreen = true;
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen || _webKitPlayer == null || _fullscreenWindow == null) return;

        _fullscreenWindow.Remove(_webKitPlayer);
        _playerContainer.PackStart(_webKitPlayer, true, true, 0);
        _webKitPlayer.ShowAll();

        _fullscreenWindow.Destroy();
        _fullscreenWindow = null;
        _isFullscreen = false;
    }

    private async Task StartEmbedPlayback(VideoItem? preloaded = null)
    {
        if (_currentVideo == null || _webKitPlayer == null) return;

        StopEmbedPlayback();

        try
        {
            var details = preloaded ?? await _api.GetVideoAsync(_currentVideo.Id);
            _currentVideoDetails = details;

            if (details == null)
            {
                return;
            }

            // Find best quality based on user preference (closest match)
            // Prefer combined streams (✓) over adaptive - they have audio built-in
            var preferredQuality = AppSettings.Instance.DefaultQuality;
            var combinedQualities = details.AvailableQualities.Where(q => !q.HasSeparateAudio).ToList();
            var adaptiveQualities = details.AvailableQualities.Where(q => q.HasSeparateAudio).ToList();


            // Find the index of the quality closest to the user's preference
            int preferredIdx = 0;
            int minDiff = int.MaxValue;
            for (int i = 0; i < details.AvailableQualities.Count; i++)
            {
                var q = details.AvailableQualities[i];
                int diff = Math.Abs(q.Resolution - preferredQuality);
                // Prefer combined streams if diff is equal
                if (diff < minDiff || (diff == minDiff && !q.HasSeparateAudio))
                {
                    minDiff = diff;
                    preferredIdx = i;
                }
            }

            // Populate quality combo
            _qualityCombo.Changed -= OnQualityChanged;
            _qualityCombo.RemoveAll();
            for (int i = 0; i < details.AvailableQualities.Count; i++)
            {
                var q = details.AvailableQualities[i];
                _qualityCombo.Append(i.ToString(), q.Label);
            }
            _qualityCombo.Active = preferredIdx;
            _qualityCombo.Changed += OnQualityChanged;

            var selectedQuality = details.AvailableQualities[preferredIdx];
            // Fallback: If adaptive but no audio, try to find a combined stream of same/similar resolution
            if (selectedQuality.HasSeparateAudio && string.IsNullOrEmpty(selectedQuality.AudioUrl))
            {
                var fallback = details.AvailableQualities.FirstOrDefault(q => !q.HasSeparateAudio && q.Resolution == selectedQuality.Resolution)
                            ?? details.AvailableQualities.FirstOrDefault(q => !q.HasSeparateAudio);
                if (fallback != null)
                {
                    Console.WriteLine($"[Player] Fallback to combined stream: {fallback.Label}");
                    selectedQuality = fallback;
                }
                else
                {
                    Console.WriteLine("[Player] No valid audio stream found for selected quality.");
                }
            }
            Console.WriteLine($"[Player] PlayVideo: VideoUrl={selectedQuality.VideoUrl}, AudioUrl={selectedQuality.AudioUrl}, Title={_currentVideo.Title}");
            _thumbnailBox.Hide();
            _webKitPlayer.Show();
            _webKitPlayer.PlayVideo(selectedQuality.VideoUrl, selectedQuality.AudioUrl, _currentVideo.Title);

            _isEmbedPlaying = true;

            // Load recommended videos
            LoadRecommendedVideos(details.RecommendedVideos);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Playback error: {ex.Message}");
        }
    }

    private void LoadRecommendedVideos(System.Collections.Generic.List<VideoItem> videos)
    {
        // Clear existing
        foreach (var child in _recommendedList.Children)
        {
            _recommendedList.Remove(child);
            child.Destroy();
        }

        if (videos == null || videos.Count == 0)
        {
            _recommendedLabel.Text = "No suggestions";
            return;
        }

        _recommendedLabel.Text = $"Suggested Videos ({videos.Count})";

        foreach (var video in videos.Take(15))  // Limit to 15
        {
            var item = new VideoListItem(video);
            item.VideoClicked += OnRecommendedVideoClicked;
            _recommendedList.PackStart(item, false, false, 0);
        }

        _recommendedList.ShowAll();
    }

    private void OnRecommendedVideoClicked(object? sender, VideoItem video)
    {
        LoadVideo(video);
    }

    private void StopEmbedPlayback()
    {
        ExitFullscreen();

        if (_webKitPlayer != null && _isEmbedPlaying)
        {
            _webKitPlayer.Hide();
            _thumbnailBox.Show();
            _isEmbedPlaying = false;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL: {ex.Message}");
        }
    }

    public async void LoadVideo(VideoItem video)
    {
        StopEmbedPlayback();

        _currentVideo = video;
        // Don't set UI elements yet - we'll set them after fetching full details
        _videoTitle.Text = video.Title; // Keep title from initial video
        _videoChannel.Text = $"{video.Channel} • {video.Views} • {video.PublishedText}"; // Keep basic info
        _videoDescription.Text = string.IsNullOrEmpty(video.Description)
            ? "..."
            : video.Description;

        // Clear old recommendations
        foreach (var child in _recommendedList.Children)
        {
            _recommendedList.Remove(child);
            child.Destroy();
        }

        // Clear old comments view
        if (_commentsView != null)
        {
            _commentsView.Destroy();
            _commentsView = null;
        }

        // Add new comments view to the comments tab
        var commentsTab = _notebook.GetNthPage(1) as Box;
        if (commentsTab != null)
        {
            _commentsView = new CommentsView(video.Id);
            commentsTab.PackStart(_commentsView, true, true, 0);
            commentsTab.ShowAll();
        }

        // Fetch and play
        var fullVideo = await _api.GetVideoAsync(video.Id);
        if (fullVideo != null)
        {
            _currentVideo = fullVideo; // Update with full details
            _currentVideo.Description = fullVideo.Description ?? "";
            _videoTitle.Text = fullVideo.Title;
            _videoChannel.Text = $"{fullVideo.Channel} • {fullVideo.Views} • {fullVideo.PublishedText}";
            _videoDescription.Text = fullVideo.Description ?? "No description";

            // Load channel image
            LoadChannelImageAsync();

            if (_webKitPlayer?.IsAvailable == true)
            {
                await StartEmbedPlayback(fullVideo);
            }
        }
        else
        {
            Console.WriteLine("Failed to load video details");
        }
    }

    private async void LoadChannelImageAsync()
    {
        if (_currentVideo == null || string.IsNullOrEmpty(_currentVideo.ChannelId)) return;

        try
        {
            // Fetch channel details to get the thumbnail
            var channel = await _api.GetChannelAsync(_currentVideo.ChannelId);
            if (channel == null || string.IsNullOrEmpty(channel.ThumbnailUrl)) return;

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var imageData = await client.GetByteArrayAsync(channel.ThumbnailUrl);
            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Gdk.Pixbuf(stream);
            var scaled = pixbuf.ScaleSimple(32, 32, Gdk.InterpType.Bilinear);

            GLib.Idle.Add(() =>
            {
                _channelImage.Pixbuf = scaled;
                return false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel image: {ex.Message}");
        }
    }

    private void OnChannelClicked(object? sender, ButtonPressEventArgs e)
    {
        if (_currentVideo?.ChannelId != null)
        {
            _mainWindow.ShowChannel(_currentVideo.ChannelId);
        }
    }

    protected override void OnDestroyed()
    {
        StopEmbedPlayback();
        base.OnDestroyed();
    }
}
