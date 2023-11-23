using Terminal.Gui;

namespace Stowage.Terminal {
    class Program {

        static IFileStorage? CreateFs(string[] args) {

            if(args.Length == 0) {
                throw new ArgumentException("no provider specified");
            }

            string provider = args[0].ToLowerInvariant();

            switch(provider) {
                case "s3":
                    // 1. bucket (required)
                    // 2. profile (optional, defaults to default)
                    // 3. region (optional, defautls to CLI profile)
                    return Files.Of.AmazonS3FromCliProfile(
                        args[1],
                        args.Length > 2 ? args[2] : "default",
                        args.Length > 3 ? args[3] : null);
            }

            return null;
        }

        static int Main(string[] args) {

            IFileStorage? fs = CreateFs(args);

            if(fs != null) {
                // system console doesn't work with text editor (TextView)
                //Application.UseSystemConsole = true;
                Application.Init();

                Application.Run(new AppTopLevel(fs));

                Application.Shutdown();
            }

            return 0;
        }
    }
}