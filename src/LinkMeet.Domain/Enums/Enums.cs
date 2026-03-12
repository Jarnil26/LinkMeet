namespace LinkMeet.Domain.Enums;

public enum UserRole
{
    Participant = 0,
    Host = 1,
    Admin = 2
}

public enum MeetingStatus
{
    Scheduled = 0,
    Active = 1,
    Ended = 2,
    Cancelled = 3
}

public enum ParticipantRole
{
    Participant = 0,
    Host = 1,
    CoHost = 2
}
