using Gtk;
using MyFirstGtkApp.Components;

public class MainWindow : Window
{
    private Overlay _overlay = null!;
    private Stack _baseStack = null!;
    private Stack _overlayStack = null!;
    private Box _backgroundPlayerContainer = null!;
    private HomeView _homeView = null!;
    private SearchView _searchView = null!;
    private VideoPlayerView _playerView = null!;
    private SettingsView _settingsView = null!;
    private ChannelView? _channelView = null!;
    private HeaderBar _header = null!;
    private Button _backBtn = null!;
    private Button _backToPlayerBtn = null!;
    private bool _isVideoFullscreen;
    private bool _isVideoPlayingInBackground;

    public MainWindow() : base("BoxyTube")
    {
        SetDefaultSize(900, 600);
        DeleteEvent += (_, __) => Application.Quit();

        BuildUI();
    }

    private void BuildUI()
    {
        // Add CSS for dark theme
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData(@"
.dark-bg {
    background-color: rgb(30, 30, 30);
}
.dark-label {
    color: white;
}
.channel-banner {
    background-color: rgb(45, 45, 45);
    border-bottom: 1px solid rgb(60, 60, 60);
}
.channel-name {
    font-size: 24px;
    font-weight: bold;
    margin-bottom: 4px;
}
.dim-label {
    color: rgb(180, 180, 180);
    font-size: 14px;
}
.section-title {
    font-size: 18px;
    font-weight: bold;
    margin-top: 16px;
    margin-bottom: 8px;
}
.verified-badge {
    color: rgb(100, 200, 255);
    font-size: 16px;
}
.stats-label {
    font-size: 13px;
    color: rgb(160, 160, 160);
}
.description-label {
    font-size: 14px;
    color: rgb(200, 200, 200);
}
");
        var screen = Gdk.Screen.Default;
        StyleContext.AddProviderForScreen(screen, cssProvider, uint.MaxValue);

        // Header Bar
        _header = new HeaderBar
        {
            Title = "BoxyTube",
            ShowCloseButton = true
        };

        // Navigation buttons
        _backBtn = new Button("←");
        _backBtn.TooltipText = "Back";
        _backBtn.Clicked += (_, __) => GoBack();
        _backBtn.NoShowAll = true;
        _backBtn.Hide();
        
        _backToPlayerBtn = new Button("🎵 Back to Player");
        _backToPlayerBtn.TooltipText = "Return to video player";
        _backToPlayerBtn.Clicked += (_, __) => ShowView("player");
        _backToPlayerBtn.NoShowAll = true;
        _backToPlayerBtn.Hide();
        
        var homeBtn = new Button("🏠 Home");
        var searchBtn = new Button("🔍 Search");
        
        homeBtn.Clicked += (_, __) => ShowView("home");
        searchBtn.Clicked += (_, __) => ShowView("search");

        _header.PackStart(_backBtn);
        _header.PackStart(_backToPlayerBtn);
        _header.PackStart(homeBtn);
        _header.PackStart(searchBtn);

        Titlebar = _header;

        // View Overlay - base is stack with player and background player
        _overlay = new Overlay
        {
            Vexpand = true,
            Hexpand = true
        };

        _baseStack = new Stack
        {
            Vexpand = true,
            Hexpand = true
        };

        _backgroundPlayerContainer = new Box(Orientation.Vertical, 0)
        {
            Vexpand = true,
            Hexpand = true
        };
        _backgroundPlayerContainer.Hide();

        _overlayStack = new Stack
        {
            TransitionType = StackTransitionType.SlideLeftRight,
            TransitionDuration = 200,
            Vexpand = true,
            Hexpand = true
        };

        // Initialize views
        _homeView = new HomeView(this);
        _searchView = new SearchView(this);
        _playerView = new VideoPlayerView(this);
        _settingsView = new SettingsView(this);

        _baseStack.AddNamed(_playerView, "player");
        _baseStack.AddNamed(_backgroundPlayerContainer, "background");
        _baseStack.VisibleChildName = "player";

        _overlayStack.AddNamed(_homeView, "home");
        _overlayStack.AddNamed(_searchView, "search");
        _overlayStack.AddNamed(_settingsView, "settings");

        _overlay.Add(_baseStack); // base layer
        _overlay.AddOverlay(_overlayStack);

        Add(_overlay);
        ShowView("home");
    }

    public void ShowView(string viewName)
    {
        // Check if we're leaving the player view
        bool currentlyOnPlayer = _baseStack.VisibleChildName == "player";
        if (currentlyOnPlayer && viewName != "player")
        {
            _isVideoPlayingInBackground = _playerView.IsPlaying;
            if (_isVideoPlayingInBackground)
            {
                _playerView.SetBackgroundMode(true);
            }
            // Always switch to background when leaving player
            _baseStack.VisibleChildName = "background";
        }
        
        // Check if we're returning to the player view
        if (viewName == "player")
        {
            _isVideoPlayingInBackground = false;
            _playerView.SetBackgroundMode(false);
            _baseStack.VisibleChildName = "player";
            _overlayStack.Hide();
        }
        else
        {
            _overlayStack.Show();
            _overlayStack.VisibleChildName = viewName;
        }
        
        // Show/hide back to player button
        if (_isVideoPlayingInBackground && viewName != "player")
        {
            _backToPlayerBtn.Show();
        }
        else
        {
            _backToPlayerBtn.Hide();
        }
        
        // Show back button in search, player, settings, or channel views
        if (viewName == "search" || viewName == "player" || viewName == "settings" || viewName == "channel")
        {
            _backBtn.Show();
        }
        else
        {
            _backBtn.Hide();
        }
    }
    
    private void GoBack()
    {
        bool currentlyOnPlayer = _baseStack.VisibleChildName == "player";
        if (currentlyOnPlayer)
        {
            // Don't stop playback, just go to home
            ShowView("home");
        }
        else
        {
            var current = _overlayStack.VisibleChildName;
            if (current == "search" || current == "settings" || current == "channel")
            {
                ShowView("home");
            }
        }
    }

    public void ShowChannel(string channelId)
    {
        if (string.IsNullOrEmpty(channelId))
        {
            Console.WriteLine("Warning: Attempted to show channel with empty/null ID");
            return;
        }

        Console.WriteLine($"Showing channel: {channelId}");

        // Check if we're leaving the player view
        bool currentlyOnPlayer = _baseStack.VisibleChildName == "player";
        if (currentlyOnPlayer)
        {
            _isVideoPlayingInBackground = _playerView.IsPlaying;
            if (_isVideoPlayingInBackground)
            {
                _playerView.SetBackgroundMode(true);
            }
            // Always switch to background when leaving player
            _baseStack.VisibleChildName = "background";
        }

        // Clean up existing channel view safely
        if (_channelView != null)
        {
            try
            {
                // Check if the view is still in the stack before removing
                if (_overlayStack.Children.Contains(_channelView))
                {
                    _overlayStack.Remove(_channelView);
                }
                _channelView.Destroy();
                _channelView = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error cleaning up old channel view: {ex.Message}");
            }
        }

        try
        {
            // Create new channel view
            _channelView = new ChannelView(this, channelId);

            // Add to overlay stack
            _overlayStack.AddNamed(_channelView, "channel");

            // Switch to channel view
            _overlayStack.VisibleChildName = "channel";
            _overlayStack.Show();

            // Update navigation buttons
            _backBtn.Show();
            if (_isVideoPlayingInBackground)
            {
                _backToPlayerBtn.Show();
            }
            else
            {
                _backToPlayerBtn.Hide();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating channel view for {channelId}: {ex.Message}");
            DialogHelper.ShowError(this, "Channel Error", $"Failed to load channel: {ex.Message}");
        }
    }
    
    public void PlayVideo(VideoItem video)
    {
        _playerView.LoadVideo(video);
        ShowView("player");
    }
    
    public void EnterVideoFullscreen()
    {
        if (_isVideoFullscreen) return;
        _isVideoFullscreen = true;
        
        // For CSD (client-side decorations), we need to hide the header
        // But GTK's Fullscreen() should handle this automatically
        Fullscreen();
    }
    
    public void ExitVideoFullscreen()
    {
        if (!_isVideoFullscreen) return;
        _isVideoFullscreen = false;
        
        Unfullscreen();
    }
    
    public bool IsVideoFullscreen => _isVideoFullscreen;
}
