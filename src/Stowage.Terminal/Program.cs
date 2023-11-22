using System.Globalization;
using Spectre.Console.Cli;
using Stowage.Terminal.Commands;
using Terminal.Gui;

namespace Stowage.Terminal {
    class Program {

        public static IFileStorage? Fs { get; set; }

        static int Main(string[] args) {

            var app = new CommandApp();
            app.Configure(config =>
                config.AddCommand<S3Command>("s3")
                );
            int code = app.Run(args);

            if(Fs != null) {
                // system console doesn't work with text editor (TextView)
                //Application.UseSystemConsole = true;
                Application.Init();

                Application.Run(new AppTopLevel(Fs));

                Application.Shutdown();
            }

            return code;
        }
    }
}