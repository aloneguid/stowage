using Stowage.WinProjFS;
using Stowage;

var provider = new StowageProvider(Files.Of.LocalDisk("c:/data"));
provider.Run();
Console.ReadKey();
