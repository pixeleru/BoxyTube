using Gtk;
using System;
using System.Collections.Generic;
using MyFirstGtkApp.Components;

public class SearchView : Box
{
    private readonly MainWindow _mainWindow;
    private readonly InvidiousApi _api;
    private SearchEntry _searchEntry = null!;
    private Box _resultsBox = null!;
    private Spinner _spinner = null!;
    private Label _statusLabel = null!;
    private uint _searchTimeout;

    public SearchView(MainWindow mainWindow) : base(Orientation.Vertical, 0)
    {
        _mainWindow = mainWindow;
        _api = new InvidiousApi();
        BuildUI();
        
        // Set dark background to match app theme
        this.StyleContext.AddClass("dark-bg");
    }

    private void BuildUI()
    {
        // Search bar area with proper styling
        var searchFrame = new Box(Orientation.Horizontal, 8)
        {
            MarginTop = 12,
            MarginBottom = 12,
            MarginStart = 12,
            MarginEnd = 12
        };

        _searchEntry = new SearchEntry
        {
            PlaceholderText = "Search YouTube via Invidious...",
            Hexpand = true
        };
        _searchEntry.Activated += (_, __) => PerformSearchAsync();
        _searchEntry.SearchChanged += OnSearchChanged;

        var searchBtn = new Button("Search");
        searchBtn.StyleContext.AddClass("suggested-action");
        searchBtn.Clicked += (_, __) => PerformSearchAsync();

        searchFrame.PackStart(_searchEntry, true, true, 0);
        searchFrame.PackStart(searchBtn, false, false, 0);

        PackStart(searchFrame, false, false, 0);

        // Separator
        PackStart(new Separator(Orientation.Horizontal), false, false, 0);

        // Loading indicator
        var loadingBox = new Box(Orientation.Horizontal, 8)
        {
            Halign = Align.Center,
            MarginTop = 8,
            MarginBottom = 8
        };
        _spinner = new Spinner();
        _statusLabel = new Label("Enter a search term to find videos");
        _statusLabel.StyleContext.AddClass("dark-label");
        loadingBox.PackStart(_spinner, false, false, 0);
        loadingBox.PackStart(_statusLabel, false, false, 0);
        PackStart(loadingBox, false, false, 0);

        // Results in a scrollable area
        var scroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never,
            Hexpand = true,
            Vexpand = true
        };

        _resultsBox = new Box(Orientation.Vertical, 0);

        scroll.Add(_resultsBox);
        PackStart(scroll, true, true, 0);
    }

    private void OnSearchChanged(object? sender, System.EventArgs e)
    {
        // Debounce search - wait 500ms after typing stops
        if (_searchTimeout > 0)
        {
            GLib.Source.Remove(_searchTimeout);
        }

        var query = _searchEntry.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            ClearResults();
            _statusLabel.Text = "Enter a search term to find videos";
            _statusLabel.Visible = true;
            return;
        }

        _searchTimeout = GLib.Timeout.Add(500, () =>
        {
            PerformSearchAsync();
            _searchTimeout = 0;
            return false;
        });
    }

    private async void PerformSearchAsync()
    {
        try
        {
            var query = _searchEntry?.Text;
            if (string.IsNullOrWhiteSpace(query))
                return;

            _spinner?.Start();
            if (_spinner != null) _spinner.Visible = true;
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Searching for \"{query}\"...";
                _statusLabel.Visible = true;
            }

            ClearResults();

            List<VideoItem> videos;
            try
            {
                videos = await _api.SearchAsync(query);
            }
            catch (Exception ex)
            {
                _spinner?.Stop();
                if (_spinner != null) _spinner.Visible = false;
                if (_statusLabel != null)
                {
                    _statusLabel.Text = "Search failed";
                    _statusLabel.Visible = true;
                }
                DialogHelper.ShowApiError(_mainWindow, "Search", ex);
                return;
            }

            _spinner?.Stop();
            if (_spinner != null) _spinner.Visible = false;

            if (videos == null || videos.Count == 0)
            {
                if (_statusLabel != null)
                {
                    _statusLabel.Text = "No videos found";
                    _statusLabel.Visible = true;
                }
                ShowEmptyState();
            }
            else
            {
                if (_statusLabel != null)
                    _statusLabel.Text = $"Found {videos.Count} videos";
                ShowResults(videos);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in PerformSearchAsync: {ex.Message}");
        }
    }

    private void ClearResults()
    {
        foreach (var child in _resultsBox.Children)
        {
            _resultsBox.Remove(child);
        }
    }

    private void ShowEmptyState()
    {
        var emptyBox = new Box(Orientation.Vertical, 12)
        {
            Valign = Align.Center,
            Halign = Align.Center,
            Vexpand = true,
            MarginTop = 48
        };
        
        var icon = new Label("🔍")
        {
            Opacity = 0.5
        };
        
        var noResults = new Label("No videos found")
        {
            Opacity = 0.7
        };
        
        emptyBox.PackStart(icon, false, false, 0);
        emptyBox.PackStart(noResults, false, false, 0);
        _resultsBox.PackStart(emptyBox, true, true, 0);
        _resultsBox.ShowAll();
    }

    private void ShowResults(List<VideoItem> videos)
    {
        foreach (var video in videos)
        {
            var row = CreateSearchResultRow(video);
            _resultsBox.PackStart(row, false, false, 0);
            
            // Add separator between items
            var sep = new Separator(Orientation.Horizontal);
            _resultsBox.PackStart(sep, false, false, 0);
        }

        _resultsBox.ShowAll();
    }

    private Widget CreateSearchResultRow(VideoItem video)
    {
        var eventBox = new EventBox();
        
        // Make it clickable to play video
        eventBox.ButtonPressEvent += (_, __) => _mainWindow.PlayVideo(video);
        eventBox.EnterNotifyEvent += (o, _) => ((EventBox)o).Window.Cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
        eventBox.LeaveNotifyEvent += (o, _) => ((EventBox)o).Window.Cursor = null;
        
        var row = new Box(Orientation.Horizontal, 12)
        {
            MarginTop = 12,
            MarginBottom = 12,
            MarginStart = 12,
            MarginEnd = 12
        };

        // Thumbnail with async loading
        var thumbFrame = new Frame
        {
            WidthRequest = 160,
            HeightRequest = 90,
            ShadowType = ShadowType.In
        };
        
        var thumbnailImage = new Image
        {
            Expand = true
        };
        thumbFrame.Add(thumbnailImage);
        
        // Load thumbnail asynchronously
        LoadThumbnailAsync(video, thumbnailImage);

        // Video info
        var info = new Box(Orientation.Vertical, 4)
        {
            Valign = Align.Center,
            Hexpand = true
        };

        var title = new Label(video.Title)
        {
            Xalign = 0,
            Ellipsize = Pango.EllipsizeMode.End,
            MaxWidthChars = 60
        };
        title.StyleContext.AddClass("h3");

        var channel = new Label(video.Channel)
        {
            Xalign = 0,
            Opacity = 0.7
        };

        var meta = new Label($"{video.Views} • {video.Duration} • {video.PublishedText}")
        {
            Xalign = 0,
            Opacity = 0.5
        };

        info.PackStart(title, false, false, 0);
        info.PackStart(channel, false, false, 0);
        info.PackStart(meta, false, false, 0);

        row.PackStart(thumbFrame, false, false, 0);
        row.PackStart(info, true, true, 0);

        eventBox.Add(row);

        return eventBox;
    }

    private async void LoadThumbnailAsync(VideoItem video, Image thumbnailImage)
    {
        try
        {
            if (video == null || string.IsNullOrEmpty(video.Id) || thumbnailImage == null)
                return;
            // Use YouTube's direct CDN for fast thumbnail loading
            var thumbnailUrl = $"https://i.ytimg.com/vi/{video.Id}/mqdefault.jpg";

            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var imageData = await client.GetByteArrayAsync(thumbnailUrl);
            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Gdk.Pixbuf(stream);
            // Scale to fit (160x90 for 16:9)
            var scaledPixbuf = pixbuf.ScaleSimple(160, 90, Gdk.InterpType.Bilinear);

            GLib.Idle.Add(() =>
            {
                try
                {
                    if (thumbnailImage != null && thumbnailImage.IsRealized && scaledPixbuf != null)
                        thumbnailImage.Pixbuf = scaledPixbuf;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting thumbnail pixbuf: {ex.Message}");
                }
                return false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading search thumbnail for {video?.Id}: {ex.Message}");
        }
    }
}
