using System.Globalization;
using Terminal.Gui;

namespace Stowage.Terminal {
    class Program {
        static async Task Main(string[] args) {
            IFileStorage fs = Files.Of.ConnectionString(args[0]);

            // system console doesn't work with text editor (TextView)
            //Application.UseSystemConsole = true;
            Application.Init();

            Application.Run(new AppTopLevel(fs));

            Application.Shutdown();
        }
    }
}