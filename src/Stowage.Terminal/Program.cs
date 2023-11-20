using System.Globalization;
using Terminal.Gui;

namespace Stowage.Terminal {
    internal class Program {
        static async Task Main(string[] args) {
            AppContext.SetSwitch("System.Globalization.Invariant", false);

            IFileStorage fs = Files.Of.ConnectionString(args[0]);

            // system console doesn't work with text editor (TextView)
            //Application.UseSystemConsole = true;
            Application.Init();

            Application.Run(new AppTopLevel(fs));

            Application.Shutdown();
        }
    }
}