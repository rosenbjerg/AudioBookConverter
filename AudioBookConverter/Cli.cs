namespace AudioBookConverter;

public static class Cli
{
    public static async Task<int> Process(string inputPath, CancellationToken cancellationToken)
    {
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
                await AaxDecrypter.ProcessSingleFile(inputPath, cancellationToken);
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
                await AudioMerger.CreateM4B(inputPath, cancellationToken);
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
    }
}