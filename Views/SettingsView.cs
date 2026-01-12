using Gtk;
using MyFirstGtkApp.Components;

public class SettingsView : Box
{
    private readonly MainWindow _mainWindow;
    private Entry _apiHostEntry = null!;
    private Switch _httpsSwitch = null!;
    private ComboBoxText _qualityCombo = null!;
    private Label _previewLabel = null!;
    private Label _statusLabel = null!;

    public SettingsView(MainWindow mainWindow) : base(Orientation.Vertical, 0)
    {
        _mainWindow = mainWindow;
        BuildUI();
        
        // Set dark background to match app theme
        this.StyleContext.AddClass("dark-bg");
    }

    private void BuildUI()
    {
        // Header (no back button - MainWindow handles that now)
        var header = new Box(Orientation.Horizontal, 8)
        {
            MarginTop = 12,
            MarginBottom = 12,
            MarginStart = 12,
            MarginEnd = 12
        };

        var titleLabel = new Label("Settings");
        titleLabel.StyleContext.AddClass("title-2");
        titleLabel.StyleContext.AddClass("dark-label");
        header.PackStart(titleLabel, false, false, 0);

        PackStart(header, false, false, 0);
        PackStart(new Separator(Orientation.Horizontal), false, false, 0);

        // Settings content in a scrollable area
        var scroll = new ScrolledWindow
        {
            VscrollbarPolicy = PolicyType.Automatic,
            HscrollbarPolicy = PolicyType.Never
        };

        var content = new Box(Orientation.Vertical, 16)
        {
            MarginTop = 24,
            MarginBottom = 24,
            MarginStart = 24,
            MarginEnd = 24
        };

        // API Settings Section
        var apiSection = CreateSection("Invidious API");

        // API Host
        var hostBox = new Box(Orientation.Horizontal, 12);
        
        var hostLabel = new Label("API Host:")
        {
            Xalign = 0,
            WidthRequest = 120
        };
        hostLabel.StyleContext.AddClass("dark-label");
        
        _apiHostEntry = new Entry
        {
            Text = AppSettings.Instance.ApiHost,
            Hexpand = true,
            PlaceholderText = "e.g., yewtu.be or 127.0.0.1:3000"
        };
        _apiHostEntry.Changed += OnSettingsChanged;

        hostBox.PackStart(hostLabel, false, false, 0);
        hostBox.PackStart(_apiHostEntry, true, true, 0);
        apiSection.PackStart(hostBox, false, false, 0);

        // HTTPS Switch
        var httpsBox = new Box(Orientation.Horizontal, 12);
        
        var httpsLabel = new Label("Use HTTPS:")
        {
            Xalign = 0,
            WidthRequest = 120
        };
        httpsLabel.StyleContext.AddClass("dark-label");
        
        _httpsSwitch = new Switch
        {
            Active = AppSettings.Instance.UseHttps,
            Valign = Align.Center
        };
        _httpsSwitch.StateSet += (sender, args) =>
        {
            OnSettingsChanged(sender, args);
            args.RetVal = false;
        };

        var httpsNote = new Label("(Use HTTP for local instances like localhost:3000)")
        {
            Xalign = 0,
            Opacity = 0.6
        };        httpsNote.StyleContext.AddClass("dark-label");
        httpsBox.PackStart(httpsLabel, false, false, 0);
        httpsBox.PackStart(_httpsSwitch, false, false, 0);
        httpsBox.PackStart(httpsNote, false, false, 12);
        apiSection.PackStart(httpsBox, false, false, 0);

        // URL Preview
        var previewBox = new Box(Orientation.Horizontal, 12)
        {
            MarginTop = 8
        };
        
        var previewTitleLabel = new Label("API URL:")
        {
            Xalign = 0,
            WidthRequest = 120
        };
        previewTitleLabel.StyleContext.AddClass("dark-label");
        
        _previewLabel = new Label(AppSettings.Instance.ApiBaseUrl)
        {
            Xalign = 0,
            Selectable = true
        };
        _previewLabel.StyleContext.AddClass("monospace");
        _previewLabel.StyleContext.AddClass("dark-label");

        previewBox.PackStart(previewTitleLabel, false, false, 0);
        previewBox.PackStart(_previewLabel, false, false, 0);
        apiSection.PackStart(previewBox, false, false, 0);

        content.PackStart(apiSection, false, false, 0);

        // Playback Settings Section
        var playbackSection = CreateSection("Playback");
        
        var qualityBox = new Box(Orientation.Horizontal, 12);
        
        var qualityLabel = new Label("Default Quality:")
        {
            Xalign = 0,
            WidthRequest = 120
        };
        qualityLabel.StyleContext.AddClass("dark-label");
        
        _qualityCombo = new ComboBoxText();
        _qualityCombo.Append("2160", "4K (2160p)");
        _qualityCombo.Append("1440", "1440p");
        _qualityCombo.Append("1080", "1080p (Recommended)");
        _qualityCombo.Append("720", "720p");
        _qualityCombo.Append("480", "480p");
        _qualityCombo.Append("360", "360p");
        _qualityCombo.Append("240", "240p");
        _qualityCombo.Append("144", "144p");
        
        // Set current value
        var currentQuality = AppSettings.Instance.DefaultQuality.ToString();
        _qualityCombo.ActiveId = currentQuality;
        if (_qualityCombo.ActiveId == null) _qualityCombo.ActiveId = "1080";
        
        _qualityCombo.Changed += OnSettingsChanged;

        var qualityNote = new Label("(Will use closest available quality)")
        {
            Xalign = 0,
            Opacity = 0.6
        };
        qualityNote.StyleContext.AddClass("dark-label");

        qualityBox.PackStart(qualityLabel, false, false, 0);
        qualityBox.PackStart(_qualityCombo, false, false, 0);
        qualityBox.PackStart(qualityNote, false, false, 12);
        playbackSection.PackStart(qualityBox, false, false, 0);

        content.PackStart(playbackSection, false, false, 0);

        // Preset buttons
        var presetsSection = CreateSection("Quick Presets");
        
        var presetsBox = new Box(Orientation.Horizontal, 8);
        
        var local = new Button("192.168.1.3:3000");
        local.Clicked += (_, __) => ApplyPreset("192.168.1.3:3000", false);
        
        var protokolla = new Button("protokolla.fi");
        protokolla.Clicked += (_, __) => ApplyPreset("invidious.protokolla.fi", true);
        
        var localhost = new Button("localhost:3000");
        localhost.Clicked += (_, __) => ApplyPreset("127.0.0.1:3000", false);

        presetsBox.PackStart(local, false, false, 0);
        presetsBox.PackStart(protokolla, false, false, 0);
        presetsBox.PackStart(localhost, false, false, 0);
        
        presetsSection.PackStart(presetsBox, false, false, 0);
        content.PackStart(presetsSection, false, false, 0);

        // Save button and status
        var actionBox = new Box(Orientation.Horizontal, 12)
        {
            MarginTop = 24
        };

        var saveBtn = new Button("Save Settings");
        saveBtn.StyleContext.AddClass("suggested-action");
        saveBtn.Clicked += OnSaveClicked;

        var testBtn = new Button("Test Connection");
        testBtn.Clicked += OnTestConnectionClicked;

        _statusLabel = new Label("")
        {
            Xalign = 0,
            Hexpand = true
        };
        _statusLabel.StyleContext.AddClass("dark-label");

        actionBox.PackStart(saveBtn, false, false, 0);
        actionBox.PackStart(testBtn, false, false, 0);
        actionBox.PackStart(_statusLabel, true, true, 0);

        content.PackStart(actionBox, false, false, 0);

        scroll.Add(content);
        PackStart(scroll, true, true, 0);
    }

    private Box CreateSection(string title)
    {
        var section = new Box(Orientation.Vertical, 8);
        
        var sectionTitle = new Label(title)
        {
            Xalign = 0
        };
        sectionTitle.StyleContext.AddClass("title-4");
        sectionTitle.StyleContext.AddClass("dark-label");
        
        section.PackStart(sectionTitle, false, false, 0);
        section.PackStart(new Separator(Orientation.Horizontal), false, false, 4);
        
        return section;
    }

    private void ApplyPreset(string host, bool useHttps)
    {
        _apiHostEntry.Text = host;
        _httpsSwitch.Active = useHttps;
        UpdatePreview();
    }

    private void OnSettingsChanged(object? sender, System.EventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var protocol = _httpsSwitch.Active ? "https" : "http";
        _previewLabel.Text = $"{protocol}://{_apiHostEntry.Text}";
    }

    private void OnSaveClicked(object? sender, System.EventArgs e)
    {
        AppSettings.Instance.ApiHost = _apiHostEntry.Text;
        AppSettings.Instance.UseHttps = _httpsSwitch.Active;
        
        // Parse and save quality
        if (int.TryParse(_qualityCombo.ActiveId, out int quality))
        {
            AppSettings.Instance.DefaultQuality = quality;
        }
        
        AppSettings.Instance.Save();
        AppSettings.Instance.NotifySettingsChanged();

        _statusLabel.Text = "✓ Settings saved!";
        _statusLabel.StyleContext.RemoveClass("error");
        
        // Clear status after 3 seconds
        GLib.Timeout.Add(3000, () =>
        {
            _statusLabel.Text = "";
            return false;
        });
    }

    private async void OnTestConnectionClicked(object? sender, System.EventArgs e)
    {
        _statusLabel.Text = "Testing connection...";
        _statusLabel.StyleContext.RemoveClass("error");

        try
        {
            var testUrl = $"{(_httpsSwitch.Active ? "https" : "http")}://{_apiHostEntry.Text}/api/v1/stats";
            
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            
            var response = await client.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
            {
                _statusLabel.Text = "✓ Connection successful!";
                _statusLabel.StyleContext.RemoveClass("error");
                DialogHelper.ShowInfo(_mainWindow, "Connection Test", "Successfully connected to the Invidious API!");
            }
            else
            {
                _statusLabel.Text = $"✗ Error: {response.StatusCode}";
                _statusLabel.StyleContext.AddClass("error");
                DialogHelper.ShowError(_mainWindow, "Connection Test Failed", $"The server returned an error: {response.StatusCode}\\n\\nPlease check the API URL.");
            }
        }
        catch (System.Exception ex)
        {
            _statusLabel.Text = $"✗ Failed: {ex.Message}";
            _statusLabel.StyleContext.AddClass("error");
            DialogHelper.ShowApiError(_mainWindow, "Connection Test", ex);
        }
    }
}
