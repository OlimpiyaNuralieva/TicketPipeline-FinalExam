string dataFolder = Path.Combine(AppContext.BaseDirectory, "data");
string? customTickets = args.FirstOrDefault(a => a != "--demo");
string ticketsPath = customTickets ?? Path.Combine(dataFolder, "tickets.csv");
string agentsPath = Path.Combine(dataFolder, "agents.csv");
string assignmentsPath = Path.Combine(AppContext.BaseDirectory, "assignments.csv");
string reportPath = Path.Combine(AppContext.BaseDirectory, "queue-report.csv");

if (args.Contains("--demo"))
{
    Demo.Run(ticketsPath);
    return;
}

const int maxEffort = 7;
Func<Ticket, int> severity = Scoring.CreateScore(Scoring.SeverityConfig);
Func<Ticket, int> quickWins = Scoring.CreateScore(Scoring.QuickWinsConfig);

Result<Ticket[]> loadedTickets = TicketFile.Load(ticketsPath)
    .Ensure(ts => ts.Select(t => t.Id).Distinct().Count() == ts.Length,
            new Error(ErrorCode.DuplicateId, $"{ticketsPath}: ticket ids must be unique"));
Result<Agent[]> loadedAgents = AgentFile.Load(agentsPath);

if (loadedTickets is Result<Ticket[]>.Fail ticketsFail)
{
    Console.WriteLine("Cannot start. " + Demo.ShowError(ticketsFail.Error));
    return;
}
if (loadedAgents is Result<Agent[]>.Fail agentsFail)
{
    Console.WriteLine("Cannot start. " + Demo.ShowError(agentsFail.Error));
    return;
}
Ticket[] tickets = ((Result<Ticket[]>.Ok)loadedTickets).Value;
Agent[] agents = ((Result<Agent[]>.Ok)loadedAgents).Value;
AssignmentBook book = AssignmentBook.Empty;

Console.WriteLine($"Loaded {tickets.Length} tickets and {agents.Length} agents.");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1 List tickets            2 Priority queue (choose policy)");
    Console.WriteLine("3 Ticket lifecycle        4 Assign ticket to agent");
    Console.WriteLine("5 Reports (workload, agent load)");
    Console.WriteLine("6 Cost report + export    7 Save assignments");
    Console.WriteLine("0 Exit");
    Console.Write("> ");
    string? choice = Console.ReadLine();
    if (choice is null || choice.Trim() == "0")
        break;

    switch (choice.Trim())
    {
        case "1":
            foreach (Ticket t in tickets)
                Console.WriteLine($"[{t.Id}] {t.Title}, {t.Kind}, effort {t.Effort}, {(t.IsOpen ? "open" : "closed")}");
            break;

        case "2":
        {
            Console.Write("Policy (s = severity, q = quick wins): ");
            Func<Ticket, int> policy = (Console.ReadLine() ?? "").Trim().ToLowerInvariant() == "q" ? quickWins : severity;
            Demo.Print("Priority queue", Pipeline.Run(tickets, maxEffort, policy));
            break;
        }

        case "3":
            if (TryReadInt("Ticket id: ", out int lifecycleId))
            {
                Console.Write("Assignee: ");
                string? assignee = Console.ReadLine();
                Console.Write("Resolution: ");
                string? resolution = Console.ReadLine();
                Demo.Report(Workflow.Process(tickets, lifecycleId, assignee, resolution));
            }
            break;

        case "4":
            if (TryReadInt("Ticket id: ", out int assignId))
            {
                Console.Write("Agent name: ");
                string? who = Console.ReadLine();
                switch (Dispatch.Assign(tickets, agents, book, assignId, who))
                {
                    case Result<AssignmentBook>.Ok ok:
                        book = ok.Value;
                        Console.WriteLine($"OK    ticket {assignId} assigned to {who?.Trim()}");
                        break;
                    case Result<AssignmentBook>.Fail fail:
                        Console.WriteLine("FAIL  " + Demo.ShowError(fail.Error) + "  (nothing was changed)");
                        break;
                }
            }
            break;

        case "5":
            Console.WriteLine("Open workload by kind:");
            foreach (WorkloadRow row in Reports.WorkloadByKind(tickets))
                Console.WriteLine($"  {row.Kind,-9} tickets: {row.Count}, total effort: {row.TotalEffort}");
            Console.WriteLine("Agent load:");
            foreach (AgentLoadRow row in Reports.AgentLoad(agents, book))
                Console.WriteLine($"  {row.Name,-6} used {row.Used}/{row.Capacity}, free {row.Free}, tickets {row.Tickets}");
            break;

        case "6":
        {
            Result<QueueReport> report = ReportWorkflow.BuildFrom(tickets, maxEffort, severity, 5, NativeCost.Real);
            Demo.ShowQueue("Cost report (native C), severity top 5", report);
            Result<string> exported = Results.Bind(report, r => ReportFile.Export(reportPath, ReportFormat.ToCsvLines(r)));
            Demo.Report(Results.Map(exported, path => "exported " + path));
            break;
        }

        case "7":
            Demo.Report(Results.Map(Dispatch.Save(assignmentsPath, book), path => $"saved {book.Items.Length} assignments to {path}"));
            break;

        default:
            Console.WriteLine("Unknown command.");
            break;
    }
}

static bool TryReadInt(string prompt, out int value)
{
    Console.Write(prompt);
    if (int.TryParse(Console.ReadLine(), out value))
        return true;
    Console.WriteLine("FAIL  a whole number is expected");
    return false;
}
