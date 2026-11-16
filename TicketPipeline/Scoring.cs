public sealed record ScoreConfig(int IncidentPoints, int RequestPoints, int QuestionPoints, int EffortFactor);

public static class Scoring
{
    public static readonly Func<ScoreConfig, Func<Ticket, int>> CreateScore = config =>
    {
        ArgumentNullException.ThrowIfNull(config);
        return ticket =>
        {
            ArgumentNullException.ThrowIfNull(ticket);
            int points = ticket.Kind switch
            {
                TicketKind.Incident => config.IncidentPoints,
                TicketKind.Request => config.RequestPoints,
                TicketKind.Question => config.QuestionPoints,
                _ => 0
            };
            return points + (Ticket.MaxEffort - ticket.Effort) * config.EffortFactor;
        };
    };

    public static readonly ScoreConfig SeverityConfig = new(30, 20, 10, 1);
    public static readonly ScoreConfig QuickWinsConfig = new(3, 2, 1, 10);

    public static readonly Func<Ticket, int> Severity = CreateScore(SeverityConfig);
    public static readonly Func<Ticket, int> QuickWins = CreateScore(QuickWinsConfig);
}
