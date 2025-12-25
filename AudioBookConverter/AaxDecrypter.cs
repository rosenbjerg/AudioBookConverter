using FFMpegCore;

namespace AudioBookConverter;

public static class AaxDecrypter
{
    public static async Task ProcessSingleFile(string filePath, CancellationToken cancellationToken)
    {
        var activationBytes = GetActivationBytes();
        var analysis = await FFProbe.AnalyseAsync(filePath, cancellationToken: cancellationToken);

        var outputFileName = Path.GetFileNameWithoutExtension(filePath) + ".m4b";
        var outputPath = Path.Combine(Path.GetDirectoryName(filePath)!, outputFileName);

        await DecryptAndConvertToM4B(filePath, outputPath, activationBytes, analysis, cancellationToken);

        Console.WriteLine($"\nSuccessfully converted: {outputFileName}");
    }

    private static string GetActivationBytes()
    {
        var activationBytes = Environment.GetEnvironmentVariable("AUDIBLE_ACTIVATION_BYTES");

        if (!string.IsNullOrWhiteSpace(activationBytes))
        {
            Console.WriteLine("Using activation bytes from AUDIBLE_ACTIVATION_BYTES environment variable");
            return activationBytes;
        }

        Console.Write("Enter Audible activation bytes (8 hex characters): ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input) || input.Length != 8)
            throw new Exception("Invalid activation bytes. Must be 8 hex characters (e.g., 1CEB00DA)");

        return input;
    }

    private static async Task DecryptAndConvertToM4B(string inputPath, string outputPath,
        string activationBytes, IMediaAnalysis analysis, CancellationToken cancellationToken)
    {
        var duration = analysis.PrimaryAudioStream?.Duration ?? TimeSpan.Zero;
        var outputFileName = Path.GetFileName(outputPath);
        var (_, top) = Console.GetCursorPosition();

        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath, true, options => options
                    .WithCustomArgument($"-activation_bytes {activationBytes}"))
                .OutputToFile(outputPath, true, options => options
                    .WithAudioCodec("copy")
                    .WithVideoCodec("copy")
                    .WithCustomArgument("-f mp4")
                    .OverwriteExisting())
                .NotifyOnProgress(percentage =>
                {
                    Console.SetCursorPosition(0, top);
                    Console.WriteLine($"Converting '{outputFileName}': {percentage:0.#}%  ");
                }, duration)
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Error during conversion: {ex.Message}");
            if (File.Exists(outputPath)) File.Delete(outputPath);
            throw;
        }
    }
}