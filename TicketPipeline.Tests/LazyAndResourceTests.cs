public class LazyAndResourceTests
{
    [Fact]
    public void ScoreLazily_CreatingTheSequenceRunsNothing_IterationDoes()
    {
        int scoreCalls = 0;
        int sourceReads = 0;
        IEnumerable<Ticket> Source()
        {
            foreach (var t in Helpers.SampleTickets()) { sourceReads++; yield return t; }
        }

        var lazy = Streams.ScoreLazily(Source(), t => { scoreCalls++; return 1; });

        Assert.Equal(0, scoreCalls);
        Assert.Equal(0, sourceReads);

        using (var e = lazy.GetEnumerator())
        {
            Assert.True(e.MoveNext());
            Assert.Equal(1, scoreCalls);
            Assert.Equal(1, sourceReads);
        }
    }

    [Fact]
    public void ScoreLazily_Materialised_IsPredictableToReuse_NotMaterialised_RepeatsTheWork()
    {
        int scoreCalls = 0;
        var tickets = Helpers.SampleTickets();
        var lazy = Streams.ScoreLazily(tickets, t => { scoreCalls++; return 1; });

        var snapshot = lazy.ToArray();
        Assert.Equal(7, scoreCalls);
        _ = snapshot.Sum(x => x.Score);
        _ = snapshot.Sum(x => x.Score);
        Assert.Equal(7, scoreCalls);

        _ = lazy.Count();
        Assert.Equal(14, scoreCalls);
    }

    [Fact]
    public void ReadLines_StoppedEarly_StillDisposesTheReader()
    {
        var reader = new TrackingReader("a\nb\nc\n");
        foreach (string line in Streams.ReadLines(() => reader))
        {
            Assert.Equal("a", line);
            break;
        }
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void ReadLines_FailureWhileReading_StillDisposesTheReader()
    {
        var reader = new FailingReader();
        Assert.Throws<IOException>(() => Streams.ReadLines(() => reader).ToArray());
        Assert.True(reader.Disposed);
    }

    [Fact]
    public void Load_ReleasesTheFileHandle_AfterSuccessAndAfterMalformedContent()
    {
        string good = TempFiles.NewPath();
        string bad = TempFiles.NewPath();
        try
        {
            File.WriteAllText(good, TempFiles.SampleCsv);
            File.WriteAllText(bad, "bad line\n");

            Helpers.ValueOf(TicketFile.Load(good));
            Helpers.ErrorOf(TicketFile.Load(bad));

            using (new FileStream(good, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            using (new FileStream(bad, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }
        finally
        {
            File.Delete(good);
            File.Delete(bad);
        }
    }
}
