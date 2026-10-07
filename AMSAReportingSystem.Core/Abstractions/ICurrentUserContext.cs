namespace AMSAReportingSystem.Core.Abstractions;

public interface ICurrentUserContext
{
    ValueTask<CurrentUserScope?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
