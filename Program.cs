using System;
using Gtk;

namespace Confluxys
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Init();
            
            var mainWindow = new MainWindow();
            mainWindow.ShowAll();
            
            Application.Run();
        }
    }
}