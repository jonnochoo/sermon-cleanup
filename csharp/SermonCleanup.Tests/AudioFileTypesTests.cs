using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class AudioFileTypesTests
{
    [Theory]
    [InlineData("sermon.wav")]
    [InlineData("sermon.mp3")]
    [InlineData("sermon.M4A")]
    [InlineData("/some/dir/sermon.flac")]
    [InlineData(@"C:\some\dir\sermon.AAC")]
    public void IsAudioFile_returns_true_for_supported_extensions(string path)
    {
        Assert.True(AudioFileTypes.IsAudioFile(path));
    }

    [Theory]
    [InlineData("readme.txt")]
    [InlineData("notes.docx")]
    [InlineData("video.mp4")]
    [InlineData("noextension")]
    public void IsAudioFile_returns_false_for_unsupported_extensions(string path)
    {
        Assert.False(AudioFileTypes.IsAudioFile(path));
    }
}
