public static class TempFiles
{
    public static string NewPath(string extension = ".csv")
        => Path.Combine(Path.GetTempPath(), "ticketpipeline-" + Guid.NewGuid().ToString("N") + extension);

    public const string SampleCsv =
        "8;WiFi;3;true;Incident\n3;Login;3;true;Incident\n5;Printer;4;true;Request\n" +
        "1;Server;7;true;Incident\n7;Email;2;false;Incident\n6;Account;2;true;Question\n9;Migration;8;true;Request\n";
}

public class TrackingReader : StringReader
{
    public bool Disposed { get; private set; }
    public TrackingReader(string text) : base(text) { }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

public class FailingReader : TrackingReader
{
    private int reads;
    public FailingReader() : base("first\nsecond\nthird\n") { }

    public override string? ReadLine()
    {
        if (reads++ == 1)
            throw new IOException("simulated disk failure");
        return base.ReadLine();
    }
}
