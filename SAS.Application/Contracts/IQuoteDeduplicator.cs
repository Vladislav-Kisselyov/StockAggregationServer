using SAS.Domain;

namespace SAS.Application.Contracts;

public interface IQuoteDeduplicator
{
    bool TryAccept(Quote quote);
    void CleanupExpired();
}
