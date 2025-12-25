namespace AudioBookConverter.Tests;

public class ChapterTitleExtractionTests
{
    [Test]
    public void ExtractChapterTitle_WithLeadingNumberAndTitle_ReturnsCleanedTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("01 - The Beginning.mp3", 1);
        Assert.That(result, Is.EqualTo("The Beginning"));
    }

    [Test]
    public void ExtractChapterTitle_WithChapterPrefixAndTitle_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Chapter 1 - Introduction.mp3", 1);
        Assert.That(result, Is.EqualTo("Introduction"));
    }

    [Test]
    public void ExtractChapterTitle_WithPartPrefix_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Part 2 - The Journey.mp3", 2);
        Assert.That(result, Is.EqualTo("The Journey"));
    }

    [Test]
    public void ExtractChapterTitle_WithMultipleSeparators_NormalizesToSingleSpaces()
    {
        var result = AudioMerger.ExtractChapterTitle("01___Chapter__One---Part_Two.mp3", 1);
        Assert.That(result, Is.EqualTo("One Part Two"));
    }

    [Test]
    public void ExtractChapterTitle_WithOnlyNumber_ReturnsFallback()
    {
        var result = AudioMerger.ExtractChapterTitle("01.mp3", 1);
        Assert.That(result, Is.EqualTo("Chapter 1"));
    }

    [Test]
    public void ExtractChapterTitle_WithShortTitle_ReturnsFallback()
    {
        var result = AudioMerger.ExtractChapterTitle("01 - To.mp3", 1);
        Assert.That(result, Is.EqualTo("Chapter 1"));
    }

    [Test]
    public void ExtractChapterTitle_WithTrackPrefix_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Track 03 - The Plot Thickens.mp3", 3);
        Assert.That(result, Is.EqualTo("The Plot Thickens"));
    }

    [Test]
    public void ExtractChapterTitle_WithSectionPrefix_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Section 5 - Climax.mp3", 5);
        Assert.That(result, Is.EqualTo("Climax"));
    }

    [Test]
    public void ExtractChapterTitle_WithChAbbreviation_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Ch 12 - Resolution.mp3", 12);
        Assert.That(result, Is.EqualTo("Resolution"));
    }

    [Test]
    public void ExtractChapterTitle_CaseInsensitive_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("CHAPTER 1 - OPENING.mp3", 1);
        Assert.That(result, Is.EqualTo("OPENING"));
    }

    [Test]
    public void ExtractChapterTitle_WithUnderscores_ReturnsCleanedTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("002_The_Great_Adventure.mp3", 2);
        Assert.That(result, Is.EqualTo("The Great Adventure"));
    }

    [Test]
    public void ExtractChapterTitle_WithDashes_ReturnsCleanedTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("03-The-Final-Chapter.mp3", 3);
        Assert.That(result, Is.EqualTo("The Final Chapter"));
    }

    [Test]
    public void ExtractChapterTitle_WithPeriods_ReturnsCleanedTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("04.The.End.mp3", 4);
        Assert.That(result, Is.EqualTo("The End"));
    }

    [Test]
    public void ExtractChapterTitle_WithMixedSeparators_ReturnsCleanedTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("05_-_.The Epilogue.mp3", 5);
        Assert.That(result, Is.EqualTo("The Epilogue"));
    }

    [Test]
    public void ExtractChapterTitle_WithBookTitleAndChapterOnly_ReturnsFallback()
    {
        var result = AudioMerger.ExtractChapterTitle("The Great Gatsby 01.mp3", 1, "The Great Gatsby");
        Assert.That(result, Is.EqualTo("Chapter 1"));
    }

    [Test]
    public void ExtractChapterTitle_WithPtAbbreviation_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Pt 7 - The Discovery.mp3", 7);
        Assert.That(result, Is.EqualTo("The Discovery"));
    }

    [Test]
    public void ExtractChapterTitle_WithChapterNoNumber_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("Chapter - Introduction.mp3", 1);
        Assert.That(result, Is.EqualTo("Introduction"));
    }

    [Test]
    public void ExtractChapterTitle_WithLeadingZeros_ReturnsTitle()
    {
        var result = AudioMerger.ExtractChapterTitle("001 - Prologue.mp3", 1);
        Assert.That(result, Is.EqualTo("Prologue"));
    }

    [Test]
    public void ExtractChapterTitle_BookTitleAndAuthor_ReturnsFallback()
    {
        var result = AudioMerger.ExtractChapterTitle("thejoyfulwisdom_00_nietzsche_64kb", 1, "The Joyful Wisdom", "Friedrich Nietzsche");
        Assert.That(result, Is.EqualTo("Chapter 1"));
    }
}