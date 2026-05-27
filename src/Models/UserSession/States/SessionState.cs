internal sealed class SessionState
{
    public UserSessionStatus Status { get; set; } = UserSessionStatus.None;

    public void Reset()
    {
        Status = UserSessionStatus.None;
    }
}
