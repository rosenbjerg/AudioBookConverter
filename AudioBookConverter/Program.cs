using AudioBookConverter;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Stopping..");
};

if (args.Length == 0)
{
    Console.WriteLine("Usage: AudioBookConverter <path>");
    Console.WriteLine("  <path> can be:");
    Console.WriteLine("    - A folder containing audio files (will be merged into m4b)");
    Console.WriteLine("    - A single .aax file");
    return 1;
}

var inputPath = args[0];
return await Cli.Process(inputPath, cts.Token);