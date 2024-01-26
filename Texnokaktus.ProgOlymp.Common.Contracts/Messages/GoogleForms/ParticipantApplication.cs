namespace Texnokaktus.ProgOlymp.Common.Contracts.Messages.GoogleForms;

public record ParticipantApplication
{
    public required int ApplicationId { get; init; }
    public required int ContestStageId { get; init; }
    public required DateTime SubmittedTime { get; init; }
    public required string AgeCategory { get; init; }
    public required string ParticipantName { get; init; }
    public required string ParticipantGrade { get; init; }
    public required string ParticipantEmail { get; init; }
    public required string School { get; init; }
    public required string SchoolRegion { get; init; }
}
