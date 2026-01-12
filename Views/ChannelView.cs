using Gtk;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFirstGtkApp.Components;

public class ChannelView : Box
{
    private readonly MainWindow _mainWindow;
    private readonly InvidiousApi _api;
    private readonly string _channelId;
    private ChannelItem? _channel;
    private int _lastBannerWidth = 0;

    // UI Components
    private Image _channelBanner = null!;
    private Image _channelThumbnail = null!;
    private Label _channelNameLabel = null!;
    private Label _verifiedBadge = null!;
    private Label _subscriberCountLabel = null!;
    private Label _videoCountLabel = null!;
    private Label _channelDescriptionLabel = null!;
    private FlowBox _videoGrid = null!;
    private Spinner _spinner = null!;
    private Box _loadingBox = null!;
    private Box _errorBox = null!;
    private Label _errorLabel = null!;

    public ChannelView(MainWindow mainWindow, string channelId) : base(Orientation.Vertical, 0)
    {
        _mainWindow = mainWindow;
        _api = new InvidiousApi();
        _channelId = channelId;

        BuildUI();
        LoadChannelAsync();

        // Set dark background
        this.StyleContext.AddClass("dark-bg");

        // Handle window resize for banner scaling
        this.SizeAllocated += OnSizeAllocated;
    }

    private void OnSizeAllocated(object sender, SizeAllocatedArgs args)
    {
        // Reload banner if size changed significantly and we have a banner loaded
        if (_channelBanner.Pixbuf != null && Math.Abs(args.Allocation.Width - _lastBannerWidth) > 100)
        {
            if (_channel != null && !string.IsNullOrEmpty(_channel.BannerUrl))
            {
                // Reload banner with new size
                LoadChannelBannerAsync();
            }
        }
    }

    private void BuildUI()
    {
        // Channel Banner Section - Displayed prominently at the top
        var bannerBox = new Box(Orientation.Vertical, 0);
        bannerBox.StyleContext.AddClass("channel-banner");

        _channelBanner = new Image
        {
            HeightRequest = 250, // Slightly taller for more prominence
            Hexpand = true,
            Valign = Align.Start
        };

        bannerBox.PackStart(_channelBanner, false, false, 0);

        // Channel Info Section - Below the banner
        var infoBox = new Box(Orientation.Horizontal, 20)
        {
            Margin = 20,
            MarginTop = 10,
            MarginBottom = 10
        };

        // Channel Avatar
        _channelThumbnail = new Image
        {
            WidthRequest = 120,
            HeightRequest = 120,
            MarginEnd = 20
        };
        _channelThumbnail.StyleContext.AddClass("channel-avatar");

        // Channel Details
        var detailsBox = new Box(Orientation.Vertical, 8)
        {
            Valign = Align.Start
        };

        // Channel Name with Verified Badge
        var nameBox = new Box(Orientation.Horizontal, 8);
        _channelNameLabel = new Label("")
        {
            Xalign = 0
        };
        _channelNameLabel.StyleContext.AddClass("channel-name");
        _channelNameLabel.StyleContext.AddClass("dark-label");

        _verifiedBadge = new Label("✓")
        {
            Xalign = 0
        };
        _verifiedBadge.StyleContext.AddClass("verified-badge");

        nameBox.PackStart(_channelNameLabel, false, false, 0);
        nameBox.PackStart(_verifiedBadge, false, false, 0);

        // Stats Row
        var statsBox = new Box(Orientation.Horizontal, 16);
        _subscriberCountLabel = new Label("")
        {
            Xalign = 0
        };
        _subscriberCountLabel.StyleContext.AddClass("stats-label");

        _videoCountLabel = new Label("")
        {
            Xalign = 0
        };
        _videoCountLabel.StyleContext.AddClass("stats-label");

        statsBox.PackStart(_subscriberCountLabel, false, false, 0);
        statsBox.PackStart(_videoCountLabel, false, false, 0);

        // Description
        _channelDescriptionLabel = new Label("")
        {
            Xalign = 0,
            Wrap = true,
            MaxWidthChars = 100,
            Lines = 2
        };
        _channelDescriptionLabel.StyleContext.AddClass("description-label");

        detailsBox.PackStart(nameBox, false, false, 0);
        detailsBox.PackStart(statsBox, false, false, 0);
        detailsBox.PackStart(_channelDescriptionLabel, false, false, 0);

        infoBox.PackStart(_channelThumbnail, false, false, 0);
        infoBox.PackStart(detailsBox, true, true, 0);

        // Videos Section
        var contentBox = new Box(Orientation.Vertical, 0)
        {
            Margin = 20
        };

        var videosLabel = new Label("Latest Videos")
        {
            Xalign = 0
        };
        videosLabel.StyleContext.AddClass("section-title");
        videosLabel.StyleContext.AddClass("dark-label");

        var scrolledWindow = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            Hexpand = true,
            Vexpand = true
        };

        _videoGrid = new FlowBox
        {
            Homogeneous = false,
            ColumnSpacing = 16,
            RowSpacing = 16,
            SelectionMode = SelectionMode.None,
            MaxChildrenPerLine = 4,
            MinChildrenPerLine = 1
        };

        scrolledWindow.Add(_videoGrid);
        contentBox.PackStart(videosLabel, false, false, 0);
        contentBox.PackStart(scrolledWindow, true, true, 0);

        // Loading State
        _loadingBox = new Box(Orientation.Vertical, 12)
        {
            Halign = Align.Center,
            Valign = Align.Center,
            Margin = 40
        };

        _spinner = new Spinner();
        _spinner.Start();

        var loadingLabel = new Label("Loading channel...")
        {
            Xalign = 0.5f
        };
        loadingLabel.StyleContext.AddClass("dark-label");

        _loadingBox.PackStart(_spinner, false, false, 0);
        _loadingBox.PackStart(loadingLabel, false, false, 0);

        // Error State
        _errorBox = new Box(Orientation.Vertical, 12)
        {
            Halign = Align.Center,
            Valign = Align.Center,
            Margin = 40
        };

        _errorLabel = new Label("")
        {
            Xalign = 0.5f,
            Wrap = true,
            MaxWidthChars = 50
        };
        _errorLabel.StyleContext.AddClass("dark-label");

        var retryBtn = new Button("Retry");
        retryBtn.StyleContext.AddClass("suggested-action");
        retryBtn.Clicked += (_, __) => LoadChannelAsync();

        _errorBox.PackStart(_errorLabel, false, false, 0);
        _errorBox.PackStart(retryBtn, false, false, 0);

        // Main Layout
        PackStart(bannerBox, false, false, 0);
        PackStart(infoBox, false, false, 0);
        PackStart(contentBox, true, true, 0);
        PackStart(_loadingBox, true, true, 0);
        PackStart(_errorBox, true, true, 0);

        ShowAll();
        _errorBox.Hide();
    }

    private async void LoadChannelAsync()
    {
        Console.WriteLine($"Loading channel: {_channelId}");

        // Show loading state
        _loadingBox.Show();
        _errorBox.Hide();

        try
        {
            _channel = await _api.GetChannelAsync(_channelId);
            if (_channel != null)
            {
                Console.WriteLine($"Channel loaded: {_channel.Name}, videos: {_channel.LatestVideos?.Count ?? 0}");
                UpdateUI();
                LoadVideos();
            }
            else
            {
                Console.WriteLine("Channel not found");
                ShowError("Channel not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel: {ex.Message}");
            ShowError($"Error loading channel: {ex.Message}");
        }
    }

    private void UpdateUI()
    {
        if (_channel == null) return;

        // Update channel info
        _channelNameLabel.Text = _channel.Name;
        _subscriberCountLabel.Text = _channel.SubscriberCountText;
        _videoCountLabel.Text = _channel.VideoCountText;
        _channelDescriptionLabel.Text = _channel.Description;

        // Show/hide verified badge
        _verifiedBadge.Visible = _channel.Verified;

        // Load banner and thumbnail
        LoadChannelBannerAsync();
        LoadChannelThumbnailAsync();

        // Hide loading
        _loadingBox.Hide();
    }

    private async void LoadChannelBannerAsync()
    {
        if (_channel == null || string.IsNullOrEmpty(_channel.BannerUrl)) return;

        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(10);
            var imageData = await client.GetByteArrayAsync(_channel.BannerUrl);

            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Gdk.Pixbuf(stream);

            // Wait for widget to be properly sized
            await Task.Delay(100); // Small delay to ensure widget is allocated

            GLib.Idle.Add(() =>
            {
                try
                {
                    // Scale banner to fit width while maintaining aspect ratio
                    var scaledWidth = Math.Max(this.AllocatedWidth > 0 ? this.AllocatedWidth : 800, 400);
                    var aspectRatio = (double)pixbuf.Height / pixbuf.Width;
                    var scaledHeight = Math.Max((int)(scaledWidth * aspectRatio), 120);

                    // Ensure dimensions are valid
                    if (scaledWidth <= 0 || scaledHeight <= 0)
                    {
                        Console.WriteLine($"Invalid banner dimensions: {scaledWidth}x{scaledHeight}, using defaults");
                        scaledWidth = 800;
                        scaledHeight = 200;
                    }

                    var scaled = pixbuf.ScaleSimple(scaledWidth, scaledHeight, Gdk.InterpType.Bilinear);

                    _channelBanner.Pixbuf = scaled;
                    _channelBanner.HeightRequest = scaledHeight;
                    _lastBannerWidth = scaledWidth;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scaling banner in idle callback: {ex.Message}");
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel banner: {ex.Message}");
            // Banner is optional, so don't show error
        }
    }

    private async void LoadChannelThumbnailAsync()
    {
        if (_channel == null || string.IsNullOrEmpty(_channel.ThumbnailUrl)) return;

        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(5);
            var imageData = await client.GetByteArrayAsync(_channel.ThumbnailUrl);

            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Gdk.Pixbuf(stream);
            var scaled = pixbuf.ScaleSimple(120, 120, Gdk.InterpType.Bilinear);

            GLib.Idle.Add(() =>
            {
                _channelThumbnail.Pixbuf = scaled;
                return false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel thumbnail: {ex.Message}");
        }
    }

    private void LoadVideos()
    {
        try
        {
            if (_channel == null) return;

            // Clear existing videos
            foreach (var child in _videoGrid.Children)
            {
                _videoGrid.Remove(child);
                child.Destroy();
            }

            if (_channel.LatestVideos == null || _channel.LatestVideos.Count == 0)
            {
                var noVideosLabel = new Label("No videos found for this channel")
                {
                    Xalign = 0.5f,
                    Margin = 40
                };
                noVideosLabel.StyleContext.AddClass("dark-label");
                _videoGrid.Add(noVideosLabel);
            }
            else
            {
                foreach (var video in _channel.LatestVideos)
                {
                    var videoCard = new VideoCard(video);
                    videoCard.Clicked += () => _mainWindow.PlayVideo(video);
                    _videoGrid.Add(videoCard);
                }
            }

            _videoGrid.ShowAll();
        }
        catch (Exception ex)
        {
            ShowError($"Error loading videos: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        _loadingBox.Hide();
        _errorLabel.Text = message;
        _errorBox.Show();
    }
}