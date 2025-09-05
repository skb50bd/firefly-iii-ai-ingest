using Microsoft.EntityFrameworkCore;
using Brotal.FireflyBuddy.Data;

namespace Brotal.FireflyBuddy.Repositories;

public class IngestMessageRepository : IIngestMessageRepository
{
    private readonly ApplicationDbContext _context;

    public IngestMessageRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IngestMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<IngestMessage>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<IngestMessage>> GetByStatusAsync(MessageStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IngestMessage> CreateAsync(IngestMessage message, CancellationToken cancellationToken = default)
    {
        _context.IngestMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<IngestMessage> UpdateAsync(IngestMessage message, CancellationToken cancellationToken = default)
    {
        _context.IngestMessages.Update(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.IngestMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            _context.IngestMessages.Remove(message);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .AnyAsync(m => m.ExternalId == externalId, cancellationToken);
    }
}

public class TransactionDraftRepository : ITransactionDraftRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionDraftRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TransactionDraft>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TransactionDraft>> GetByStatusAsync(DraftStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionDraft> CreateAsync(TransactionDraft draft, CancellationToken cancellationToken = default)
    {
        _context.TransactionDrafts.Add(draft);
        await _context.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<TransactionDraft> UpdateAsync(TransactionDraft draft, CancellationToken cancellationToken = default)
    {
        _context.TransactionDrafts.Update(draft);
        await _context.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _context.TransactionDrafts.FindAsync([id], cancellationToken);
        if (draft != null)
        {
            _context.TransactionDrafts.Remove(draft);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<TransactionDraft>> GetPendingDraftsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .Where(d => d.Status == DraftStatus.Pending || d.Status == DraftStatus.Ready)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly ApplicationDbContext _context;

    public AnalysisResultRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResult?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisResults
            .Include(a => a.IngestMessage)
            .FirstOrDefaultAsync(a => a.IngestMessageId == messageId, cancellationToken);
    }

    public async Task<AnalysisResult> CreateAsync(AnalysisResult result, CancellationToken cancellationToken = default)
    {
        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<AnalysisResult> UpdateAsync(AnalysisResult result, CancellationToken cancellationToken = default)
    {
        _context.AnalysisResults.Update(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
