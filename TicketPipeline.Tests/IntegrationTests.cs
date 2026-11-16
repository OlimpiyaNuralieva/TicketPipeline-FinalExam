public class IntegrationTests
{
    private const int MaxEffort = 7;

    private static string WriteSample(string path)
    {
        File.WriteAllText(path, TempFiles.SampleCsv);
        return path;
    }

    [Fact]
    public void NormalPath_ReportsAndNativeCallAreIntegrated()
    {
        string input = WriteSample(TempFiles.NewPath());
        try
        {
            var report = Helpers.ValueOf(ReportWorkflow.Build(input, MaxEffort, Scoring.Severity, 5, NativeCost.Real));

            Assert.Equal(new[] { 3, 8, 1, 5, 6 }, report.Priority.Select(r => r.Id).ToArray());
            Assert.Equal(new[] { 18075, 18075, 40066, 14300, 4100 }, report.Priority.Select(r => r.CostCents).ToArray());
            Assert.Equal(94616L, report.TotalCostCents);
            Assert.Equal(new[] { TicketKind.Incident, TicketKind.Request, TicketKind.Question }, report.Workload.Select(w => w.Kind).ToArray());
        }
        finally { File.Delete(input); }
    }

    [Fact]
    public void NormalPath_Export_WritesTheReportFile()
    {
        string input = WriteSample(TempFiles.NewPath());
        string output = TempFiles.NewPath();
        try
        {
            Assert.Equal(output, Helpers.ValueOf(ReportWorkflow.BuildAndExport(input, output, MaxEffort, Scoring.Severity, 2, NativeCost.Real)));

            Assert.Equal(new[]
            {
                "rank;id;title;score;cost_cents",
                "1;3;Login;35;18075",
                "2;8;WiFi;35;18075",
                "TOTAL;;;;36150"
            }, File.ReadAllLines(output));
        }
        finally { File.Delete(input); File.Delete(output); }
    }

    [Fact]
    public void FailurePath_NativeUnavailable_ReportsTheProblemAndExportsNothing()
    {
        string input = WriteSample(TempFiles.NewPath());
        string output = TempFiles.NewPath();
        try
        {
            Func<int, int, int> failing = (k, e) => throw new DllNotFoundException("simulated");

            var error = Helpers.ErrorOf(ReportWorkflow.BuildAndExport(input, output, MaxEffort, Scoring.Severity, 5, failing));

            Assert.Equal(ErrorCode.NativeUnavailable, error.Code);
            Assert.False(File.Exists(output));
            Assert.Equal(TempFiles.SampleCsv, File.ReadAllText(input));
        }
        finally { File.Delete(input); File.Delete(output); }
    }

    [Fact]
    public void FailurePath_DuplicateIds_AreRejectedByEnsure()
    {
        string input = TempFiles.NewPath();
        try
        {
            File.WriteAllText(input, "1;A;2;true;Question\n1;B;3;true;Request\n");

            var error = Helpers.ErrorOf(ReportWorkflow.Build(input, MaxEffort, Scoring.Severity, 5, NativeCost.Real));

            Assert.Equal(ErrorCode.DuplicateId, error.Code);
            Assert.Contains(input, error.Message);
        }
        finally { File.Delete(input); }
    }

    [Fact]
    public void FailurePath_ExportFolderMissing_ReportsWriteFailure()
    {
        string input = WriteSample(TempFiles.NewPath());
        string output = Path.Combine(Path.GetTempPath(), "no-such-" + Guid.NewGuid().ToString("N"), "r.csv");
        try
        {
            var error = Helpers.ErrorOf(ReportWorkflow.BuildAndExport(input, output, MaxEffort, Scoring.Severity, 5, NativeCost.Real));
            Assert.Equal(ErrorCode.FileWriteFailed, error.Code);
        }
        finally { File.Delete(input); }
    }
}
