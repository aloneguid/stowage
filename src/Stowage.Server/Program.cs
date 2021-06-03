using System;
using System.IO.Files.Server.FTP;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetBox.Terminal.App;
using Serilog;
using Zhaobang.FtpServer;
using Zhaobang.FtpServer.Authenticate;
using Zhaobang.FtpServer.Connections;

namespace System.IO.Files.Server
{
   class Program
   {
      static int Main(string[] args)
      {
         using Serilog.Core.Logger log = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
         Log.Logger = log;
         Stowage.Files.SetLogger(msg => Log.Information($"SW|{msg}"));

         var app = new Application("File Server");

         app.Command("ftp", cmd =>
         {
            cmd.Description = "Serve on FTP protocol.";
            LinePrimitive<string> cs = cmd.Argument<string>("cs", "connection string");

            cmd.OnExecute(async () =>
            {
               var endpoint = new IPEndPoint(IPAddress.Any, 21);
               var server = new FtpServer(endpoint, 
                  new FileProviderFactory(cs), 
                  new LocalDataConnectionFactory(), 
                  new AnonymousAuthenticator());
               var cts = new CancellationTokenSource();
               Task runResult = server.RunAsync(cts.Token);

               Console.ReadLine();
               cts.Cancel();
               await runResult;
            });
         });

         app.Command("azblob", cmd =>
         {
            cmd.Description = "Emulate Azure Blob Storage Protocol";
            LinePrimitive<int> port = cmd.Argument<int>("p", "port number", 8910);
            LinePrimitive<string> cs = cmd.Argument<string>("cs", "connection string");

            cmd.OnExecute(async () =>
            {
               throw new NotImplementedException();
            });
         });

         return app.Execute();
      }
   }
}