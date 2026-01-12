using Gtk;

class Program
{
    static void Main(string[] args)
    {
        Application.Init();

        var win = new MainWindow();
        win.ShowAll();
        
        Application.Run();
    }
}
