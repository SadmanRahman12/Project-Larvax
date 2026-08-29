using LarvaX.Core.Interfaces;

namespace LarvaX.Infrastructure.Services;

public class NullSmsService : ISmsService
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        // Assumption: production SMS integration is provider-specific and should be swapped via ISmsService.
        return Task.CompletedTask;
    }
}
