namespace MyFirstGtkApp.Components
{
using System;
using Gtk;

/// <summary>
/// Helper class for showing dialogs to the user
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Show an error dialog
    /// </summary>
    public static void ShowError(Window? parent, string title, string message)
    {
        GLib.Idle.Add(() =>
        {
            var dialog = new MessageDialog(
                parent,
                DialogFlags.Modal | DialogFlags.DestroyWithParent,
                MessageType.Error,
                ButtonsType.Ok,
                message
            );
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();
            return false;
        });
    }
    
    /// <summary>
    /// Show a warning dialog
    /// </summary>
    public static void ShowWarning(Window? parent, string title, string message)
    {
        GLib.Idle.Add(() =>
        {
            var dialog = new MessageDialog(
                parent,
                DialogFlags.Modal | DialogFlags.DestroyWithParent,
                MessageType.Warning,
                ButtonsType.Ok,
                message
            );
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();
            return false;
        });
    }
    
    /// <summary>
    /// Show an info dialog
    /// </summary>
    public static void ShowInfo(Window? parent, string title, string message)
    {
        GLib.Idle.Add(() =>
        {
            var dialog = new MessageDialog(
                parent,
                DialogFlags.Modal | DialogFlags.DestroyWithParent,
                MessageType.Info,
                ButtonsType.Ok,
                message
            );
            dialog.Title = title;
            dialog.Run();
            dialog.Destroy();
            return false;
        });
    }
    
    /// <summary>
    /// Show a confirmation dialog and return the result
    /// </summary>
    public static bool ShowConfirm(Window? parent, string title, string message)
    {
        var dialog = new MessageDialog(
            parent,
            DialogFlags.Modal | DialogFlags.DestroyWithParent,
            MessageType.Question,
            ButtonsType.YesNo,
            message
        );
        dialog.Title = title;
        var result = dialog.Run();
        dialog.Destroy();
        return result == (int)ResponseType.Yes;
    }
    
    /// <summary>
    /// Show an error dialog for API/network errors
    /// </summary>
    public static void ShowApiError(Window? parent, string operation, Exception ex)
    {
        var message = ex.Message;
        
        // Make common errors more user-friendly
        if (message.Contains("Timeout") || message.Contains("timed out"))
        {
            message = "Connection timed out. Please check your internet connection and try again.";
        }
        else if (message.Contains("No such host") || message.Contains("Name or service not known"))
        {
            message = "Could not connect to the server. Please check your API URL in Settings.";
        }
        else if (message.Contains("Connection refused"))
        {
            message = "Connection refused. Make sure the Invidious server is running.";
        }
        else if (message.Contains("403") || message.Contains("Forbidden"))
        {
            message = "Access denied. The video may be restricted or unavailable.";
        }
        else if (message.Contains("404") || message.Contains("Not Found"))
        {
            message = "The requested content was not found.";
        }
        else if (message.Contains("500") || message.Contains("Internal Server Error"))
        {
            message = "Server error. Please try again later or check the Invidious instance.";
        }
        
        ShowError(parent, $"Error: {operation}", message);
    }
}
}
