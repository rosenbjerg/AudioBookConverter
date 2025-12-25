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
if (File.Exists(inputPath))
{
    var fileExtension = Path.GetExtension(inputPath);
    if (fileExtension != ".aax")
    {
        Console.WriteLine($"Error: Expected a .aax file, but got: {fileExtension}");
        return 1;
    }

    try
    {
        await AaxDecrypter.ProcessSingleFile(inputPath, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Conversion cancelled");
    }
}
else if (Directory.Exists(inputPath))
{
    try
    {
        await AudioMerger.CreateM4B(inputPath, cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Conversion cancelled");
    }
}
else
{
    Console.WriteLine($"Error: Path does not exist: {inputPath}");
    return 1;
}


return 0;