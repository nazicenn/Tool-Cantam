namespace tool_cantam.Services.ErrorCodes;

public static class PingErrorCodes
{
    public const string InvalidTarget = "L.Ping.Error.InvalidTarget";
    public const string TargetNotFound = "L.Ping.Error.TargetNotFound";
    public const string InvalidCount = "L.Ping.Error.InvalidCount";
    public const string InvalidTimeout = "L.Ping.Error.InvalidTimeout";
    public const string NoReply = "L.Ping.Error.NoReply";
    public const string IcmpBlocked = "L.Ping.Error.IcmpBlocked"; 
    public const string Unexpected = "L.Common.Error.Unexpected";
    public const string Cancelled = "L.Common.Error.Cancelled";
}