public static class Demo
{
    public static void Run(string ticketsPath)
    {
        string dataFolder = Path.Combine(AppContext.BaseDirectory, "data");

        Console.WriteLine("--- Normal path: load tickets.csv ---");
        Ticket[] tickets;
        switch (TicketFile.Load(ticketsPath))
        {
            case Result<Ticket[]>.Ok loadedOk:
                tickets = loadedOk.Value;
                Console.WriteLine($"OK    loaded {tickets.Length} tickets");
                break;
            case Result<Ticket[]>.Fail loadedFail:
                Console.WriteLine("FAIL  " + ShowError(loadedFail.Error));
                return;
            default:
                throw new InvalidOperationException("unknown case");
        }

        const int maxEffort = 7;
        Print("SeverityFirst", Pipeline.Run(tickets, maxEffort, Pipeline.SeverityFirst));
        Print("QuickWinsFirst", Pipeline.Run(tickets, maxEffort, Pipeline.QuickWinsFirst));
        Print("Empty dataset", Pipeline.Run(Array.Empty<Ticket>(), maxEffort, Pipeline.SeverityFirst));

        Console.WriteLine("--- Normal path: Result chain ---");
        Report(Workflow.Process(tickets, 3, "Anna", "restarted router"));

        Console.WriteLine("--- Domain failure 1: invalid ticket data ---");
        Report(Results.Map(
            Ticket.Create(-5, "", 99, true, TicketKind.Incident),
            t => $"created ticket {t.Id}"));

        Console.WriteLine("--- Domain failure 2: required ticket is missing ---");
        Report(Workflow.Process(tickets, 99, "Anna", "restarted router"));

        Console.WriteLine("--- Domain failure 3: step not allowed in this state ---");
        switch (Pipeline.FindTicket(tickets, 3))
        {
            case Option<Ticket>.Some foundForState:

                Result<TicketState> skippedAssign = Results.Bind(
                    Workflow.Begin(foundForState.Value),
                    s => Workflow.Resolve(s, "done"));
                Report(Results.Map(skippedAssign, s => Workflow.Describe(s)));
                break;
            case Option<Ticket>.None:
                Console.WriteLine("Demo skipped: ticket 3 is not in the data.");
                break;
        }

        Console.WriteLine("--- Lookup: present, present with IsOpen = false, missing ---");
        ShowLookup(Pipeline.FindTicket(tickets, 3), 3);
        ShowLookup(Pipeline.FindTicket(tickets, 7), 7);
        ShowLookup(Pipeline.FindTicket(tickets, 99), 99);

        Console.WriteLine("--- File failure 1: missing file ---");
        ReportLoad(TicketFile.Load(Path.Combine(dataFolder, "missing.csv")));

        Console.WriteLine("--- File failure 2: malformed content ---");
        ReportLoad(TicketFile.Load(Path.Combine(dataFolder, "malformed.csv")));

        Console.WriteLine($"--- Data after the file failures: still {tickets.Length} tickets, nothing was changed ---");

        Func<Ticket, int> severity = Scoring.CreateScore(Scoring.SeverityConfig);
        Func<Ticket, int> quickWins = Scoring.CreateScore(Scoring.QuickWinsConfig);

        Console.WriteLine("--- Report 1: open workload by kind ---");
        foreach (WorkloadRow row in Reports.WorkloadByKind(tickets))
            Console.WriteLine($"{row.Kind,-9} tickets: {row.Count}, total effort: {row.TotalEffort}");

        ShowQueue("Report 2 + native cost: severity-weighted, top 5", ReportWorkflow.BuildFrom(tickets, maxEffort, severity, 5, NativeCost.Real));
        ShowQueue("Report 2 + native cost: quick-wins-weighted, top 3", ReportWorkflow.BuildFrom(tickets, maxEffort, quickWins, 3, NativeCost.Real));

        Console.WriteLine("--- Native call: ticket_cost_cents(Incident, 7) ---");
        switch (NativeCost.Calculate(TicketKind.Incident, 7, NativeCost.Real))
        {
            case Result<int>.Ok nativeOk:
                Console.WriteLine($"OK    {nativeOk.Value} cents = {Money(nativeOk.Value)}");
                break;
            case Result<int>.Fail nativeFail:
                Console.WriteLine("FAIL  " + ShowError(nativeFail.Error));
                break;
        }

        Console.WriteLine("--- Deferred execution and snapshot ---");
        int scoreCalls = 0;
        Func<Ticket, int> countingScore = t => { scoreCalls++; return severity(t); };
        IEnumerable<ScoredTicket> lazyScores = Streams.ScoreLazily(tickets.Workable(maxEffort), countingScore);
        Console.WriteLine($"After creating the sequence: {scoreCalls} score calls");
        ScoredTicket[] snapshot = lazyScores.ToArray();
        Console.WriteLine($"After ToArray (iteration): {scoreCalls} score calls");
        int snapshotSum = snapshot.Sum(x => x.Score) + snapshot.Sum(x => x.Score);
        Console.WriteLine($"After using the snapshot twice (sum {snapshotSum}): {scoreCalls} score calls");

        Console.WriteLine("--- Workflow: reports + native call + export ---");
        string exportPath = Path.Combine(AppContext.BaseDirectory, "queue-report.csv");
        Report(Results.Map(
            ReportWorkflow.BuildAndExport(ticketsPath, exportPath, maxEffort, severity, 5, NativeCost.Real),
            path => "exported " + path));
        if (File.Exists(exportPath))
            foreach (string line in File.ReadAllLines(exportPath))
                Console.WriteLine("      " + line);

        Console.WriteLine("--- Failure: native: native library cannot be loaded (simulated by a failing function) ---");
        Func<int, int, int> missingLibrary = (_, _) => throw new DllNotFoundException("simulated: ticketcost was not found");
        string failedExportPath = Path.Combine(AppContext.BaseDirectory, "queue-report-failed.csv");
        Report(ReportWorkflow.BuildAndExport(ticketsPath, failedExportPath, maxEffort, severity, 5, missingLibrary));
        Console.WriteLine($"Export file created: {File.Exists(failedExportPath)}");

        Console.WriteLine("--- Failure: report: report file cannot be written ---");
        Report(ReportWorkflow.BuildAndExport(ticketsPath, Path.Combine(dataFolder, "no-such-folder", "r.csv"), maxEffort, severity, 5, NativeCost.Real));

        Console.WriteLine("--- Final: assignment workflow (Agent capacity) ---");
        switch (AgentFile.Load(Path.Combine(dataFolder, "agents.csv")))
        {
            case Result<Agent[]>.Ok agentsOk:
                Agent[] agents = agentsOk.Value;
                AssignmentBook book = AssignmentBook.Empty;
                foreach ((int id, string who) in new[] { (5, "Chen"), (8, "Chen"), (99, "Anna"), (3, "Nobody"), (7, "Anna"), (3, "Anna") })
                {
                    Result<AssignmentBook> step = Dispatch.Assign(tickets, agents, book, id, who);
                    switch (step)
                    {
                        case Result<AssignmentBook>.Ok stepOk:
                            book = stepOk.Value;
                            Console.WriteLine($"OK    ticket {id} -> {who}");
                            break;
                        case Result<AssignmentBook>.Fail stepFail:
                            Console.WriteLine($"FAIL  ticket {id} -> {who}: {ShowError(stepFail.Error)}");
                            break;
                    }
                }
                Console.WriteLine("--- Final: report 3, agent load ---");
                foreach (AgentLoadRow row in Reports.AgentLoad(agents, book))
                    Console.WriteLine($"{row.Name,-6} used {row.Used}/{row.Capacity}, free {row.Free}, tickets {row.Tickets}");
                break;
            case Result<Agent[]>.Fail agentsFail:
                Console.WriteLine("FAIL  " + ShowError(agentsFail.Error));
                break;
        }
        Console.WriteLine("--- Final: malformed agents file ---");
        switch (AgentFile.Load(Path.Combine(dataFolder, "malformed-agents.csv")))
        {
            case Result<Agent[]>.Ok:
                Console.WriteLine("OK    (unexpected)");
                break;
            case Result<Agent[]>.Fail malformedFail:
                Console.WriteLine("FAIL  " + ShowError(malformedFail.Error));
                break;
        }
    }

    internal static string ShowError(Error error) => $"[{error.Code}] {error.Message}";

    internal static void Report(Result<string> result)
    {
        switch (result)
        {
            case Result<string>.Ok success:
                Console.WriteLine("OK    " + success.Value);
                break;
            case Result<string>.Fail failure:
                Console.WriteLine("FAIL  " + ShowError(failure.Error));
                break;
        }
    }

    internal static void ReportLoad(Result<Ticket[]> result)
    {
        switch (result)
        {
            case Result<Ticket[]>.Ok success:
                Console.WriteLine($"OK    loaded {success.Value.Length} tickets");
                break;
            case Result<Ticket[]>.Fail failure:
                Console.WriteLine("FAIL  " + ShowError(failure.Error));
                break;
        }
    }

    internal static void ShowLookup(Option<Ticket> option, int id)
    {
        switch (option)
        {
            case Option<Ticket>.Some some:
                Console.WriteLine($"Found:   [{some.Value.Id}] {some.Value.Title}, open: {some.Value.IsOpen}");
                break;
            case Option<Ticket>.None:
                Console.WriteLine($"Missing: no ticket with id {id}");
                break;
        }
    }

    internal static void Print(string title, Recommendation[] recs)
    {
        Console.WriteLine($"--- {title} ---");
        if (recs.Length == 0)
        {
            Console.WriteLine("No recommendations.");
            return;
        }
        foreach (var r in recs)
            Console.WriteLine($"{r.Rank}. [{r.Id}] {r.Title} ({r.Score})");
    }

    internal static string Money(long cents) => $"{cents / 100}.{cents % 100:D2}";

    internal static void ShowQueue(string title, Result<QueueReport> result)
    {
        Console.WriteLine($"--- {title} ---");
        switch (result)
        {
            case Result<QueueReport>.Ok ok:
                foreach (PricedRow row in ok.Value.Priority)
                    Console.WriteLine($"{row.Rank}. [{row.Id}] {row.Title} score {row.Score}, cost {Money(row.CostCents)}");
                Console.WriteLine($"Total cost: {Money(ok.Value.TotalCostCents)}");
                break;
            case Result<QueueReport>.Fail fail:
                Console.WriteLine("FAIL  " + ShowError(fail.Error));
                break;
        }
    }
}
