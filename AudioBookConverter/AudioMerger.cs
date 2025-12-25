using System.Text.RegularExpressions;
using FFMpegCore;

namespace AudioBookConverter;

public static partial class AudioMerger
{
    public static async Task CreateM4B(string dirPath, CancellationToken cancellationToken)
    {
        var audioFiles = DiscoverAudioFiles(dirPath);
        var analysedAudioFiles = await AnalyseFiles(audioFiles, cancellationToken);
        var coverImage = await FindOrExtractCoverImage(dirPath, analysedAudioFiles, cancellationToken);

        var outputFileName = Path.GetFileName(dirPath) + ".m4b";
        var outputPath = Path.Combine(Path.GetDirectoryName(dirPath)!, outputFileName);
        await ConvertToM4B(analysedAudioFiles, coverImage, outputPath, cancellationToken);
    }

    private static string[] DiscoverAudioFiles(string dirPath)
    {
        var audioFileExtensions = new[] { ".mp3", ".m4a", ".m4b" };
        var audioFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories)
            .Where(f => audioFileExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .ToArray();

        return audioFiles.Length == 0
            ? throw new Exception($"No audio files found in the directory: {dirPath}")
            : audioFiles;
    }

    private static async Task<AudioFile[]> AnalyseFiles(string[] audioFiles, CancellationToken cancellationToken)
    {
        var files = new List<AudioFile>();
        foreach (var audioFile in audioFiles)
        {
            var analysis = await FFProbe.AnalyseAsync(audioFile, cancellationToken: cancellationToken);
            files.Add(new AudioFile(audioFile, analysis));
        }

        return files.ToArray();
    }

    private static async Task<string> FindOrExtractCoverImage(string dirPath, AudioFile[] audioFiles, CancellationToken cancellationToken)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var coverImageFile = Directory
            .EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories)
            .FirstOrDefault(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()) &&
                                 Path.GetFileNameWithoutExtension(f).Contains("cover", StringComparison.CurrentCultureIgnoreCase));

        if (coverImageFile != null)
        {
            if (Path.GetExtension(coverImageFile).ToLower() == ".webp")
            {
                var jpegPath = Path.Combine(dirPath, "cover.jpg");
                await FFMpegArguments
                    .FromFileInput(coverImageFile)
                    .OutputToFile(jpegPath, true, options => options
                        .WithVideoCodec("mjpeg")
                        .WithCustomArgument("-q:v 2"))
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                return jpegPath;
            }

            return coverImageFile;
        }

        var audioFileWithImage = audioFiles.FirstOrDefault(audioFile => audioFile.Analysis.PrimaryVideoStream != null);
        if (audioFileWithImage != null)
        {
            var extractedCoverImageFile = Path.Combine(dirPath, "cover.jpg");
            await FFMpegArguments
                .FromFileInput(audioFileWithImage.Path)
                .OutputToFile(extractedCoverImageFile, true, options => options
                    .WithCustomArgument("-an")
                    .WithVideoCodec("mjpeg")
                    .WithCustomArgument("-frames:v 1")
                    .WithCustomArgument("-q:v 2"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            return extractedCoverImageFile;
        }

        throw new Exception("No cover image found in folder or embedded in audio files");
    }

    private static async Task ConvertToM4B(AudioFile[] audioFiles, string coverImage, string outputPath,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileNameWithoutExtension(outputPath);
        var metadataBuilder = PrepareMetadata(audioFiles, fileName);

        var totalDuration = TimeSpan.FromSeconds(audioFiles.Sum(c => c.Analysis.PrimaryAudioStream!.Duration.TotalSeconds));
        var outputFileName = Path.GetFileName(outputPath);
        var (_, top) = Console.GetCursorPosition();
        var bitrate = audioFiles.Max(audioFile => (int)audioFile.Analysis.PrimaryAudioStream!.BitRate);
        var sampleRate = audioFiles.Max(audioFile => audioFile.Analysis.PrimaryAudioStream!.SampleRateHz);

        try
        {
            await FFMpegArguments
                .FromDemuxConcatInput(audioFiles.Select(audioFile => audioFile.Path))
                .AddFileInput(coverImage)
                .AddMetaData(metadataBuilder)
                .OutputToFile(outputPath, true, options => options
                    .WithCustomArgument("-map 0:a -map 1:v")
                    .WithAudioCodec("aac")
                    .WithAudioBitrate(bitrate)
                    .WithAudioSamplingRate(sampleRate)
                    .WithCustomArgument("-c:v copy -disposition:v:0 attached_pic")
                    .OverwriteExisting())
                .NotifyOnProgress(percentage =>
                {
                    Console.SetCursorPosition(0, top);
                    Console.WriteLine($"Creating '{outputFileName}': {percentage:0.#}%  ");
                }, totalDuration)
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Error during conversion: {ex.Message}");
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    private static FFMetadataBuilder PrepareMetadata(AudioFile[] audioFiles, string fileName)
    {
        var fileNameDetails = fileName.Split("-");
        var author = fileNameDetails[0].Trim();
        var title = fileNameDetails[1].Trim();
        var metadataBuilder = new FFMetadataBuilder();

        metadataBuilder.WithTag("artist", author);
        metadataBuilder.WithTag("album_artist", author);
        metadataBuilder.WithTag("album", title);
        metadataBuilder.WithTag("genre", "Audiobook");

        var chapter = 1;
        foreach (var audioFile in audioFiles)
        {
            var audioFileStream = audioFile.Analysis.PrimaryAudioStream;
            var chapterTitle = ExtractChapterTitle(audioFile.Path, chapter, title);
            metadataBuilder.WithChapter(chapterTitle, audioFileStream!.Duration.TotalSeconds);
            chapter++;
        }

        return metadataBuilder;
    }

    public static string ExtractChapterTitle(string filePath, int chapterNumber, string? bookTitle = null, string? bookAuthor = null)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        var cleanedTitle = LeadingNumbersRegex().Replace(fileName, "");
        cleanedTitle = ChapterPrefixRegex().Replace(cleanedTitle, "");
        cleanedTitle = TrailingNumbersRegex().Replace(cleanedTitle, "");
        cleanedTitle = QualityIndicatorRegex().Replace(cleanedTitle, "");
        cleanedTitle = WhitespaceRegex().Replace(cleanedTitle, " ").Trim();

        if (!string.IsNullOrWhiteSpace(bookTitle))
        {
            var originalConcatenated = bookTitle.Replace(" ", "");
            var normalizedTitle = NormalizeForComparison(bookTitle);
            var normalizedConcatenated = normalizedTitle.Replace(" ", "");

            cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{Regex.Escape(originalConcatenated)}\b", "", RegexOptions.IgnoreCase).Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{Regex.Escape(normalizedConcatenated)}\b", "", RegexOptions.IgnoreCase).Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, Regex.Escape(bookTitle), "", RegexOptions.IgnoreCase).Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, Regex.Escape(normalizedTitle), "", RegexOptions.IgnoreCase).Trim();

            foreach (var word in normalizedTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (word.Length > 2)
                    cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{Regex.Escape(word)}\b", "", RegexOptions.IgnoreCase).Trim();

            cleanedTitle = WhitespaceRegex().Replace(cleanedTitle, " ").Trim();
        }

        if (!string.IsNullOrWhiteSpace(bookAuthor))
        {
            var normalizedAuthor = NormalizeForComparison(bookAuthor);
            var concatenatedAuthor = normalizedAuthor.Replace(" ", "");

            cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{Regex.Escape(concatenatedAuthor)}\b", "", RegexOptions.IgnoreCase).Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, Regex.Escape(normalizedAuthor), "", RegexOptions.IgnoreCase).Trim();

            foreach (var word in normalizedAuthor.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (word.Length > 2)
                    cleanedTitle = Regex.Replace(cleanedTitle, $@"\b{Regex.Escape(word)}\b", "", RegexOptions.IgnoreCase).Trim();

            cleanedTitle = WhitespaceRegex().Replace(cleanedTitle, " ").Trim();
        }

        if (!string.IsNullOrWhiteSpace(cleanedTitle) && cleanedTitle.Length > 3)
        {
            if (bookTitle != null && cleanedTitle.Equals(bookTitle, StringComparison.OrdinalIgnoreCase)) return $"Chapter {chapterNumber}";
            return cleanedTitle;
        }

        return $"Chapter {chapterNumber}";
    }

    private static string NormalizeForComparison(string text)
    {
        var normalized = Regex.Replace(text, @"\b(the|a|an)\b", "", RegexOptions.IgnoreCase);
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();
        return normalized;
    }

    [GeneratedRegex(@"^\d+[\s\-_.]*")]
    private static partial Regex LeadingNumbersRegex();

    [GeneratedRegex(@"^(Chapter|Part|Section|Track|Ch|Pt)[\s\-_.]*\d*[\s\-_.]*", RegexOptions.IgnoreCase)]
    private static partial Regex ChapterPrefixRegex();

    [GeneratedRegex(@"[\s\-_.]*\d+$")]
    private static partial Regex TrailingNumbersRegex();

    [GeneratedRegex(@"[\s\-_.]*\d+k?b(ps)?[\s\-_.]*", RegexOptions.IgnoreCase)]
    private static partial Regex QualityIndicatorRegex();

    [GeneratedRegex(@"[\s\-_.]+")]
    private static partial Regex WhitespaceRegex();

    private class AudioFile(string path, IMediaAnalysis analysis)
    {
        public string Path { get; } = path;
        public IMediaAnalysis Analysis { get; } = analysis;
    }
}