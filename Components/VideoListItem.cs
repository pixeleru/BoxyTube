using Gtk;
using System;

/// <summary>
/// A horizontal video list item for sidebars (recommended videos, search results).
/// Shows: [Thumbnail] Title
///                    Channel • Views
/// </summary>
public class VideoListItem : EventBox
{
    private readonly VideoItem _video;
    public event EventHandler<VideoItem>? VideoClicked;
    
    public VideoListItem(VideoItem video)
    {
        _video = video;
        BuildUI();
        
        // Make clickable
        Events |= Gdk.EventMask.ButtonPressMask;
        ButtonPressEvent += OnClicked;
        
        // Hover effect
        Events |= Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
        EnterNotifyEvent += (s, e) => StyleContext.AddClass("hover");
        LeaveNotifyEvent += (s, e) => StyleContext.RemoveClass("hover");
    }
    
    private void BuildUI()
    {
        var container = new Box(Orientation.Horizontal, 8)
        {
            MarginTop = 4,
            MarginBottom = 4,
            MarginStart = 4,
            MarginEnd = 4
        };
        
        // Thumbnail (120x68 for 16:9 aspect)
        var thumbnailFrame = new Frame()
        {
            ShadowType = ShadowType.None
        };
        
        var thumbnail = new Image()
        {
            WidthRequest = 120,
            HeightRequest = 68
        };
        
        // Load thumbnail async
        LoadThumbnailAsync(thumbnail);
        
        // Duration overlay
        var thumbnailOverlay = new Overlay();
        thumbnailOverlay.Add(thumbnail);
        
        var durationLabel = new Label(_video.Duration)
        {
            Halign = Align.End,
            Valign = Align.End,
            MarginEnd = 4,
            MarginBottom = 4
        };
        durationLabel.StyleContext.AddClass("osd");
        thumbnailOverlay.AddOverlay(durationLabel);
        
        thumbnailFrame.Add(thumbnailOverlay);
        container.PackStart(thumbnailFrame, false, false, 0);
        
        // Info section
        var infoBox = new Box(Orientation.Vertical, 2)
        {
            Valign = Align.Center
        };
        
        // Title (max 2 lines)
        var title = new Label(_video.Title)
        {
            Xalign = 0,
            Wrap = true,
            Lines = 2,
            Ellipsize = Pango.EllipsizeMode.End,
            MaxWidthChars = 30
        };
        title.StyleContext.AddClass("caption");
        
        // Channel and views
        var meta = new Label($"{_video.Channel}")
        {
            Xalign = 0,
            Opacity = 0.7
        };
        meta.StyleContext.AddClass("dim-label");
        
        var views = new Label($"{_video.Views}")
        {
            Xalign = 0,
            Opacity = 0.6
        };
        views.StyleContext.AddClass("dim-label");
        
        infoBox.PackStart(title, false, false, 0);
        infoBox.PackStart(meta, false, false, 0);
        infoBox.PackStart(views, false, false, 0);
        
        container.PackStart(infoBox, true, true, 0);
        
        Add(container);
    }
    
    private async void LoadThumbnailAsync(Image imageWidget)
    {
        try
        {
            var pixbuf = await ThumbnailCache.LoadAsync(_video.Id, 120, 68);
            
            if (pixbuf != null)
            {
                GLib.Idle.Add(() =>
                {
                    imageWidget.Pixbuf = pixbuf;
                    return false;
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load thumbnail: {ex.Message}");
        }
    }
    
    [GLib.ConnectBefore]
    private void OnClicked(object sender, ButtonPressEventArgs e)
    {
        if (e.Event.Type == Gdk.EventType.ButtonPress)
        {
            VideoClicked?.Invoke(this, _video);
        }
    }
}
