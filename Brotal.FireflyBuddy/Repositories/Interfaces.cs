using Brotal.FireflyBuddy.Data;

namespace Brotal.FireflyBuddy.Repositories;

public interface IIngestMessageRepository
{
    Task<Data.IngestMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Data.IngestMessage>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Data.IngestMessage>> GetByStatusAsync(Data.MessageStatus status, CancellationToken cancellationToken = default);
    Task<Data.IngestMessage> CreateAsync(Data.IngestMessage message, CancellationToken cancellationToken = default);
    Task<Data.IngestMessage> UpdateAsync(Data.IngestMessage message, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
}

public interface ITransactionDraftRepository
{
    Task<Data.TransactionDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Data.TransactionDraft>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Data.TransactionDraft>> GetByStatusAsync(Data.DraftStatus status, CancellationToken cancellationToken = default);
    Task<Data.TransactionDraft> CreateAsync(Data.TransactionDraft draft, CancellationToken cancellationToken = default);
    Task<Data.TransactionDraft> UpdateAsync(Data.TransactionDraft draft, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Data.TransactionDraft>> GetPendingDraftsAsync(CancellationToken cancellationToken = default);
}

public interface IAnalysisResultRepository
{
    Task<Data.AnalysisResult?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<Data.AnalysisResult> CreateAsync(Data.AnalysisResult result, CancellationToken cancellationToken = default);
    Task<Data.AnalysisResult> UpdateAsync(Data.AnalysisResult result, CancellationToken cancellationToken = default);
}
