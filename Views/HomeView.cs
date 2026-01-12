using Gtk;
using System;
using System.Collections.Generic;
using MyFirstGtkApp.Components;

public class HomeView : Box
{
    private readonly MainWindow _mainWindow;
    private readonly InvidiousApi _api;
    private FlowBox _videoGrid = null!;
    private Spinner _spinner = null!;
    private Label _statusLabel = null!;
    private Label _apiStatusLabel = null!;
    private Button? _activeNavButton = null;

    public HomeView(MainWindow mainWindow) : base(Orientation.Horizontal, 0)
    {
        _mainWindow = mainWindow;
        _api = new InvidiousApi();
        BuildUI();
        
        // Set dark background to match app theme
        this.StyleContext.AddClass("dark-bg");
        
        // Subscribe to settings changes
        AppSettings.Instance.SettingsChanged += OnSettingsChanged;
    }
    
    private void OnSettingsChanged()
    {
        // Update API status label
        try
        {
            _apiStatusLabel.Text = $"🔗 {new Uri(AppSettings.Instance.ApiBaseUrl).Host}";
        }
        catch
        {
            _apiStatusLabel.Text = $"🔗 {AppSettings.Instance.ApiHost}";
        }
        
        // Reload content with new API
        LoadTrendingAsync("music");
    }

    private void BuildUI()
    {
        // ═══════════════════════════════════════════════════════════
        // LEFT SIDEBAR: Navigation
        // ═══════════════════════════════════════════════════════════
        var sidebar = new Box(Orientation.Vertical, 0)
        {
            WidthRequest = 180
        };
        sidebar.StyleContext.AddClass("sidebar");

        // App title/logo
        var logoBox = new Box(Orientation.Horizontal, 8)
        {
            MarginTop = 16,
            MarginBottom = 16,
            MarginStart = 16,
            MarginEnd = 16
        };

        sidebar.PackStart(new Separator(Orientation.Horizontal), false, false, 0);

        // Navigation section
        var navLabel = new Label("Browse")
        {
            Xalign = 0,
            MarginStart = 16,
            MarginTop = 12,
            MarginBottom = 8
        };
        navLabel.StyleContext.AddClass("dim-label");
        navLabel.StyleContext.AddClass("dark-label");
        sidebar.PackStart(navLabel, false, false, 0);

        // Navigation buttons
        var navItems = new (string icon, string label, string? category)[]
        {
            ("🏠", "Home Feed", null),
            ("🎵", "Music", "music"),
            ("🎮", "Gaming", "gaming"),
            ("📰", "News", "news"),
            ("🎬", "Movies", "movies"),
        };

        foreach (var (icon, label, category) in navItems)
        {
            var btn = CreateNavButton(icon, label, category);
            sidebar.PackStart(btn, false, false, 0);
        }

        sidebar.PackStart(new Separator(Orientation.Horizontal) { MarginTop = 12, MarginBottom = 12 }, false, false, 0);

        // Settings section
        var settingsLabel = new Label("Settings")
        {
            Xalign = 0,
            MarginStart = 16,
            MarginBottom = 8
        };
        settingsLabel.StyleContext.AddClass("dim-label");
        settingsLabel.StyleContext.AddClass("dark-label");
        sidebar.PackStart(settingsLabel, false, false, 0);

        var settingsBtn = CreateNavButton("🔩", "Preferences", "settings");
        sidebar.PackStart(settingsBtn, false, false, 0);

        // Spacer
        sidebar.PackStart(new Box(Orientation.Vertical, 0) { Vexpand = true }, true, true, 0);

        // API status at bottom
        _apiStatusLabel = new Label($"🔗 {new Uri(AppSettings.Instance.ApiBaseUrl).Host}")
        {
            Xalign = 0,
            MarginStart = 16,
            MarginBottom = 16,
            Opacity = 0.6
        };
        _apiStatusLabel.StyleContext.AddClass("dark-label");
        _apiStatusLabel.StyleContext.AddClass("dim-label");
        sidebar.PackEnd(_apiStatusLabel, false, false, 0);

        PackStart(sidebar, false, false, 0);
        PackStart(new Separator(Orientation.Vertical), false, false, 0);

        // ═══════════════════════════════════════════════════════════
        // RIGHT SIDE: Video feed
        // ═══════════════════════════════════════════════════════════
        var contentPane = new Box(Orientation.Vertical, 0)
        {
            Hexpand = true
        };

        // Top bar with search and refresh
        var topBar = new Box(Orientation.Horizontal, 8)
        {
            MarginTop = 12,
            MarginBottom = 12,
            MarginStart = 16,
            MarginEnd = 16
        };

        // Refresh button
        var refreshBtn = new Button("↻");
        refreshBtn.TooltipText = "Refresh";
        refreshBtn.Clicked += (s, e) => LoadTrendingAsync();
        topBar.PackEnd(refreshBtn, false, false, 0);

        // Loading indicator
        _spinner = new Spinner();
        topBar.PackEnd(_spinner, false, false, 0);

        _statusLabel = new Label("");
        _statusLabel.StyleContext.AddClass("dark-label");
        topBar.PackEnd(_statusLabel, false, false, 0);

        contentPane.PackStart(topBar, false, false, 0);
        contentPane.PackStart(new Separator(Orientation.Horizontal), false, false, 0);

        // Scrollable video grid
        var scroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            Vexpand = true
        };

        _videoGrid = new FlowBox
        {
            Valign = Align.Start,
            MaxChildrenPerLine = 4,
            MinChildrenPerLine = 2,
            SelectionMode = SelectionMode.None,
            Homogeneous = true,
            RowSpacing = 16,
            ColumnSpacing = 16,
            MarginTop = 16,
            MarginBottom = 16,
            MarginStart = 16,
            MarginEnd = 16
        };

        scroll.Add(_videoGrid);
        contentPane.PackStart(scroll, true, true, 0);

        PackStart(contentPane, true, true, 0);

        // Load default content
        LoadTrendingAsync("music");
    }

    private Button CreateNavButton(string icon, string label, string? action)
    {
        var btn = new Button();
        var box = new Box(Orientation.Horizontal, 8)
        {
            MarginStart = 8,
            MarginEnd = 8
        };
        box.PackStart(new Label(icon), false, false, 0);
        box.PackStart(new Label(label) { Xalign = 0 }, true, true, 0);
        btn.Add(box);
        btn.Relief = ReliefStyle.None;

        btn.Clicked += (s, e) =>
        {
            // Update active button styling
            _activeNavButton?.StyleContext.RemoveClass("suggested-action");
            btn.StyleContext.AddClass("suggested-action");
            _activeNavButton = btn;

            if (action == "settings")
            {
                _mainWindow.ShowView("settings");
            }
            else
            {
                LoadTrendingAsync(action);
            }
        };

        return btn;
    }

    private async void LoadTrendingAsync(string? type = null)
    {
        _spinner.Start();
        _spinner.Visible = true;

        // Clear existing
        foreach (var child in _videoGrid.Children)
        {
            _videoGrid.Remove(child);
            child.Destroy();
        }

        List<VideoItem> videos;

        try
        {
            if (type != null)
            {
                videos = await _api.SearchAsync(type);
                _statusLabel.Text = $"{videos.Count} results";
            }
            else
            {
                videos = await _api.GetTrendingAsync();
                _statusLabel.Text = $"{videos.Count} trending";
            }
        }
        catch (Exception ex)
        {
            _spinner.Stop();
            _spinner.Visible = false;
            _statusLabel.Text = "Failed";
            DialogHelper.ShowApiError(_mainWindow, "Loading Videos", ex);
            return;
        }

        _spinner.Stop();
        _spinner.Visible = false;

        if (videos.Count == 0)
        {
            _statusLabel.Text = "No videos found";
        }
        else
        {
            foreach (var video in videos)
            {
                var card = new VideoCard(video);
                card.Clicked += () => _mainWindow.PlayVideo(video);
                _videoGrid.Add(card);
            }
            _videoGrid.ShowAll();
        }
    }

    private async System.Threading.Tasks.Task LoadSearchAsync(string query)
    {
        _spinner.Start();
        _spinner.Visible = true;
        _statusLabel.Text = "Searching...";

        // Clear existing
        foreach (var child in _videoGrid.Children)
        {
            _videoGrid.Remove(child);
            child.Destroy();
        }

        try
        {
            var videos = await _api.SearchAsync(query);
            _statusLabel.Text = $"{videos.Count} results for \"{query}\"";

            _spinner.Stop();
            _spinner.Visible = false;

            foreach (var video in videos)
            {
                var card = new VideoCard(video);
                card.Clicked += () => _mainWindow.PlayVideo(video);
                _videoGrid.Add(card);
            }
            _videoGrid.ShowAll();
        }
        catch (Exception ex)
        {
            _spinner.Stop();
            _spinner.Visible = false;
            _statusLabel.Text = "Search failed";
            DialogHelper.ShowApiError(_mainWindow, "Search", ex);
        }
    }
}
