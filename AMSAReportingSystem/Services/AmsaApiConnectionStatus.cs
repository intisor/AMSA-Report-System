namespace AMSAReportingSystem.Services;

public class AmsaApiConnectionStatus
{
    public bool IsChecked { get; private set; }
    public bool IsConnected { get; private set; }
    public string Message { get; private set; } = "Checking AMSA API...";
    public DateTimeOffset? LastCheckedAt { get; private set; }

    public void MarkConnected(string? message = null)
    {
        IsChecked = true;
        IsConnected = true;
        LastCheckedAt = DateTimeOffset.UtcNow;
        Message = string.IsNullOrWhiteSpace(message) ? "Connected to AMSA API" : message;
    }

    public void MarkFailed(string? message = null)
    {
        IsChecked = true;
        IsConnected = false;
        LastCheckedAt = DateTimeOffset.UtcNow;
        Message = string.IsNullOrWhiteSpace(message) ? "AMSA API unavailable" : message;
    }
}
