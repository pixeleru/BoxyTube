namespace MyFirstGtkApp.Components
{
using Gtk;
using System;

public class ChannelCard : EventBox
{
    public event System.Action? Clicked;
    private readonly ChannelItem _channel;
    private Image _thumbnailImage = null!;

    public ChannelCard(ChannelItem channel)
    {
        _channel = channel;
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

        // Channel thumbnail
        var thumbFrame = new Frame
        {
            HeightRequest = 112
        };
        thumbFrame.StyleContext.AddClass("view");

        _thumbnailImage = new Image
        {
            Expand = true
        };

        thumbFrame.Add(_thumbnailImage);

        // Channel info
        var infoBox = new Box(Orientation.Vertical, 2)
        {
            Margin = 4
        };

        var name = new Label(_channel.Name)
        {
            Xalign = 0,
            Ellipsize = Pango.EllipsizeMode.End,
            Lines = 1,
            MaxWidthChars = 24
        };
        name.StyleContext.AddClass("dark-label");

        var subscriberCount = new Label(_channel.SubscriberCountText)
        {
            Xalign = 0
        };
        subscriberCount.StyleContext.AddClass("dim-label");
        subscriberCount.StyleContext.AddClass("dark-label");

        var videoCount = new Label(_channel.VideoCountText)
        {
            Xalign = 0
        };
        videoCount.StyleContext.AddClass("dim-label");
        videoCount.StyleContext.AddClass("dark-label");

        infoBox.PackStart(name, false, false, 0);
        infoBox.PackStart(subscriberCount, false, false, 0);
        infoBox.PackStart(videoCount, false, false, 0);

        card.PackStart(thumbFrame, false, false, 0);
        card.PackStart(infoBox, false, false, 0);

        Add(card);
    }

    private async void LoadThumbnailAsync()
    {
        try
        {
            var pixbuf = await ThumbnailCache.LoadAsync(_channel.Id, 200, 112);
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
            Console.WriteLine($"Error loading channel thumbnail for {_channel.Id}: {ex.Message}");
        }
    }
}
}