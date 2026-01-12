namespace MyFirstGtkApp.Components
{
using Gtk;
using System;

public class VideoCard : EventBox
{
    public event System.Action? Clicked;
    private readonly VideoItem _video;
    private Image _thumbnailImage = null!;

    public VideoCard(VideoItem video)
    {
        _video = video;
        BuildUI();
        LoadThumbnailAsync();

        // Make clickable
        Events = Gdk.EventMask.ButtonPressMask;
        ButtonPressEvent += (_, __) => Clicked?.Invoke();
    }

    private void BuildUI()
    {
        var card = new Box(Orientation.Vertical, 4)
        {
            WidthRequest = 200
        };

        // Thumbnail with overlay
        var thumbFrame = new Frame
        {
            HeightRequest = 112
        };
        thumbFrame.StyleContext.AddClass("view");

        var thumbBox = new Overlay();
        
        _thumbnailImage = new Image
        {
            Expand = true
        };

        var durationLabel = new Label(_video.Duration)
        {
            Halign = Align.End,
            Valign = Align.End,
            MarginEnd = 4,
            MarginBottom = 4
        };
        durationLabel.StyleContext.AddClass("dim-label");
        durationLabel.StyleContext.AddClass("dark-label");

        thumbBox.Add(_thumbnailImage);
        thumbBox.AddOverlay(durationLabel);
        thumbFrame.Add(thumbBox);

        // Video info
        var infoBox = new Box(Orientation.Vertical, 2)
        {
            Margin = 4
        };

        var title = new Label(_video.Title)
        {
            Xalign = 0,
            Ellipsize = Pango.EllipsizeMode.End,
            Lines = 2,
            MaxWidthChars = 24
        };
        title.StyleContext.AddClass("dark-label");

        var channel = new Label(_video.Channel)
        {
            Xalign = 0
        };
        channel.StyleContext.AddClass("dim-label");
        channel.StyleContext.AddClass("dark-label");

        var views = new Label($"{_video.Views} views")
        {
            Xalign = 0
        };
        views.StyleContext.AddClass("dim-label");
        views.StyleContext.AddClass("dark-label");

        infoBox.PackStart(title, false, false, 0);
        infoBox.PackStart(channel, false, false, 0);
        infoBox.PackStart(views, false, false, 0);

        card.PackStart(thumbFrame, false, false, 0);
        card.PackStart(infoBox, false, false, 0);

        Add(card);
    }

    private async void LoadThumbnailAsync()
    {
        try
        {
            var pixbuf = await ThumbnailCache.LoadAsync(_video.Id, 200, 112);
            
            if (pixbuf != null)
            {
                GLib.Idle.Add(() =>
                {
                    _thumbnailImage.Pixbuf = pixbuf;
                    return false;
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading thumbnail for {_video.Id}: {ex.Message}");
        }
    }}
}
