namespace LarvaX.Core.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
