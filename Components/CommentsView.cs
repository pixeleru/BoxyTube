using Gtk;
using System;
using System.Collections.Generic;

public class CommentsView : ScrolledWindow
{
    private readonly InvidiousApi _api;
    private readonly string _videoId;
    private Box _commentsBox = null!;

    public CommentsView(string videoId) : base()
    {
        _api = new InvidiousApi();
        _videoId = videoId;
        BuildUI();
        LoadCommentsAsync();
    }

    private void BuildUI()
    {
        _commentsBox = new Box(Orientation.Vertical, 8)
        {
            Margin = 12
        };

        Add(_commentsBox);
        ShowAll();
    }

    private async void LoadCommentsAsync()
    {
        try
        {
            var comments = await _api.GetCommentsAsync(_videoId);
            UpdateComments(comments);
        }
        catch (Exception ex)
        {
            ShowError($"Error loading comments: {ex.Message}");
        }
    }

    private void UpdateComments(List<CommentItem> comments)
    {
        if (comments.Count == 0)
        {
            var noCommentsLabel = new Label("No comments yet")
            {
                Xalign = 0.5f,
                Yalign = 0.5f
            };
            noCommentsLabel.StyleContext.AddClass("dim-label");
            _commentsBox.PackStart(noCommentsLabel, true, true, 0);
            ShowAll();
            return;
        }

        foreach (var comment in comments)
        {
            var commentWidget = CreateCommentWidget(comment);
            _commentsBox.PackStart(commentWidget, false, false, 0);
        }

        ShowAll();
    }

    private Widget CreateCommentWidget(CommentItem comment)
    {
        var commentBox = new Box(Orientation.Horizontal, 8)
        {
            MarginStart = 8,
            MarginEnd = 8
        };

        // Avatar widget (Image or placeholder Box)
        Widget avatarWidget;

        if (!string.IsNullOrEmpty(comment.AuthorThumbnailUrl))
        {
            avatarWidget = new Image
            {
                WidthRequest = 32,
                HeightRequest = 32
            };
            LoadAuthorThumbnailAsync((Image)avatarWidget, comment.AuthorThumbnailUrl);
        }
        else
        {
            // Fallback to placeholder
            var avatarBox = new Box(Orientation.Vertical, 0)
            {
                WidthRequest = 32,
                HeightRequest = 32
            };
            avatarBox.StyleContext.AddClass("avatar-placeholder");

            var avatarLabel = new Label(comment.Author.Substring(0, 1).ToUpper())
            {
                Xalign = 0.5f,
                Yalign = 0.5f
            };
            avatarBox.PackStart(avatarLabel, true, true, 0);
            avatarWidget = avatarBox;
        }

        var contentBox = new Box(Orientation.Vertical, 4);

        // Author and metadata
        var headerBox = new Box(Orientation.Horizontal, 8);

        var authorLabel = new Label(comment.Author)
        {
            Xalign = 0
        };
        if (comment.IsAuthorChannelOwner)
        {
            authorLabel.StyleContext.AddClass("channel-owner");
        }

        var timeLabel = new Label(comment.PublishedText)
        {
            Xalign = 0
        };
        timeLabel.StyleContext.AddClass("dim-label");

        headerBox.PackStart(authorLabel, false, false, 0);
        headerBox.PackStart(timeLabel, false, false, 0);

        // Comment content
        var contentLabel = new Label(comment.Content)
        {
            Xalign = 0,
            Wrap = true,
            Selectable = true
        };

        // Likes
        var likesLabel = new Label(comment.LikeCountText)
        {
            Xalign = 0
        };
        likesLabel.StyleContext.AddClass("dim-label");

        contentBox.PackStart(headerBox, false, false, 0);
        contentBox.PackStart(contentLabel, false, false, 0);
        contentBox.PackStart(likesLabel, false, false, 0);

        commentBox.PackStart(avatarWidget, false, false, 0);
        commentBox.PackStart(contentBox, true, true, 0);

        return commentBox;
    }

    private async void LoadAuthorThumbnailAsync(Image image, string url)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var imageData = await client.GetByteArrayAsync(url);
            using var stream = new System.IO.MemoryStream(imageData);
            var pixbuf = new Gdk.Pixbuf(stream);
            var scaled = pixbuf.ScaleSimple(32, 32, Gdk.InterpType.Bilinear);

            GLib.Idle.Add(() =>
            {
                image.Pixbuf = scaled;
                return false;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading author thumbnail: {ex.Message}");
            // Thumbnail loading failed, but comment will still show with placeholder
        }
    }

    private void ShowError(string message)
    {
        var errorLabel = new Label(message)
        {
            Xalign = 0.5f,
            Yalign = 0.5f
        };
        errorLabel.StyleContext.AddClass("error");
        _commentsBox.PackStart(errorLabel, true, true, 0);
        ShowAll();
    }
}