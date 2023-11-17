using Spectre.Console;

public static class Program {
    public static async Task Main(string[] args) {
        AnsiConsole.Markup("[underline red]Hello[/] World!");
    }
}