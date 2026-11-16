public class FileTests
{
    private static IEnumerable<string> LinesThatFail()
    {
        yield return "new line";
        throw new IOException("simulated failure while writing");
    }

    [Fact]
    public void Export_FailureWhileWriting_KeepsTheOldFileAndLeavesNoTempFile()
    {
        string path = NewTempPath();
        try
        {
            File.WriteAllText(path, "old report");

            var error = Helpers.ErrorOf(ReportFile.Export(path, LinesThatFail()));

            Assert.Equal(ErrorCode.FileWriteFailed, error.Code);
            Assert.Equal("old report", File.ReadAllText(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".tmp");
        }
    }

    private static string NewTempPath()
        => Path.Combine(Path.GetTempPath(), "ticketpipeline-" + Guid.NewGuid().ToString("N") + ".csv");

    [Fact]
    public void Load_MissingFile_ReportsTheProblemWithThePathAndCreatesNothing()
    {
        string path = NewTempPath();

        var error = Helpers.ErrorOf(TicketFile.Load(path));

        Assert.Equal(ErrorCode.FileMissing, error.Code);
        Assert.Contains(path, error.Message);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Load_MalformedContent_FailsWithLineNumbersAndDoesNotChangeTheFile()
    {
        string path = NewTempPath();
        string content = "1;A;2;true;Question\nbad line\n3;C;9;true;Request\n";
        try
        {
            File.WriteAllText(path, content);

            var error = Helpers.ErrorOf(TicketFile.Load(path));

            Assert.Equal(ErrorCode.DataMalformed, error.Code);
            Assert.Contains(path, error.Message);
            Assert.Contains("line 2:", error.Message);
            Assert.Contains("line 3:", error.Message);
            Assert.Equal(content, File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_EmptyPath_FailsWithFileUnreadable_InsteadOfThrowing()
    {
        var error = Helpers.ErrorOf(TicketFile.Load(""));

        Assert.Equal(ErrorCode.FileUnreadable, error.Code);
    }
}
