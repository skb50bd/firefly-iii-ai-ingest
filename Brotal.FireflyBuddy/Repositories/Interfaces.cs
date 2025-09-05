using Brotal.FireflyBuddy.Data;

namespace Brotal.FireflyBuddy.Repositories;

public interface IIngestMessageRepository
{
    Task<IngestMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<IngestMessage>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<IngestMessage>> GetByStatusAsync(MessageStatus status, CancellationToken cancellationToken = default);
    Task<IngestMessage> CreateAsync(IngestMessage message, CancellationToken cancellationToken = default);
    Task<IngestMessage> UpdateAsync(IngestMessage message, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
}

public interface ITransactionDraftRepository
{
    Task<TransactionDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionDraft>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionDraft>> GetByStatusAsync(DraftStatus status, CancellationToken cancellationToken = default);
    Task<TransactionDraft> CreateAsync(TransactionDraft draft, CancellationToken cancellationToken = default);
    Task<TransactionDraft> UpdateAsync(TransactionDraft draft, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TransactionDraft>> GetPendingDraftsAsync(CancellationToken cancellationToken = default);
}

public interface IAnalysisResultRepository
{
    Task<AnalysisResult?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<AnalysisResult> CreateAsync(AnalysisResult result, CancellationToken cancellationToken = default);
    Task<AnalysisResult> UpdateAsync(AnalysisResult result, CancellationToken cancellationToken = default);
}
