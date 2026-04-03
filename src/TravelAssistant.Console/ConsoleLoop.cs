namespace TravelAssistant.Console;

public sealed class ConsoleLoop
{
    public static bool IsExitCommand(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        string trimmed = input.Trim().ToLowerInvariant();
        return trimmed is "exit" or "quit" or "q";
    }

    public static async Task RunAsync(
        Func<string, Task<string>> handleMessage,
        CancellationToken cancellationToken = default)
    {
        PrintBanner();

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write("\nYou: ");
            string? input = System.Console.ReadLine();

            if (input is null || IsExitCommand(input))
            {
                System.Console.WriteLine("\nGoodbye! Safe travels.");
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
                continue;

            try
            {
                string response = await handleMessage(input);
                System.Console.WriteLine($"\nAssistant: {response}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }
    }

    private static void PrintBanner()
    {
        System.Console.WriteLine("============================================");
        System.Console.WriteLine("  Intelligent Travel Planning Assistant");
        System.Console.WriteLine("  Powered by Microsoft Agent Framework");
        System.Console.WriteLine("  Type 'exit' or 'quit' to stop.");
        System.Console.WriteLine("============================================");
    }
}
