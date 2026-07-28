using SAS.Domain;

namespace SAS.Application.Contracts;

public interface IQuoteProcessor
{
    Task ProcessAsync(
        Quote quote,
        CancellationToken cancellationToken);
}
