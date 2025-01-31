using Grey;
using Stowage;
using Stowage.Desktop;
using static Grey.App;

if(args.Length == 0) return;

string connectionString = args[0];
Console.WriteLine($"Connection String: {connectionString}");
IFileStorage fs = Files.Of.ConnectionString(connectionString);

var ev = new EntriesView(fs);
ev.RefreshFolder();

App.Run("Stowage Warehouse", () => {
    ev.RenderFrame();
    return true;
});
