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

    public async Task<Data.IngestMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Data.IngestMessage>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Data.IngestMessage>> GetByStatusAsync(Data.MessageStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.IngestMessages
            .Include(m => m.AnalysisResult)
            .Include(m => m.TransactionDraft)
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Data.IngestMessage> CreateAsync(Data.IngestMessage message, CancellationToken cancellationToken = default)
    {
        _context.IngestMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<Data.IngestMessage> UpdateAsync(Data.IngestMessage message, CancellationToken cancellationToken = default)
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

    public async Task<Data.TransactionDraft?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Data.TransactionDraft>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Data.TransactionDraft>> GetByStatusAsync(Data.DraftStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TransactionDrafts
            .Include(d => d.IngestMessage)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Data.TransactionDraft> CreateAsync(Data.TransactionDraft draft, CancellationToken cancellationToken = default)
    {
        _context.TransactionDrafts.Add(draft);
        await _context.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public async Task<Data.TransactionDraft> UpdateAsync(Data.TransactionDraft draft, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<Data.TransactionDraft>> GetPendingDraftsAsync(CancellationToken cancellationToken = default)
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

    public async Task<Data.AnalysisResult?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _context.AnalysisResults
            .Include(a => a.IngestMessage)
            .FirstOrDefaultAsync(a => a.IngestMessageId == messageId, cancellationToken);
    }

    public async Task<Data.AnalysisResult> CreateAsync(Data.AnalysisResult result, CancellationToken cancellationToken = default)
    {
        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<Data.AnalysisResult> UpdateAsync(Data.AnalysisResult result, CancellationToken cancellationToken = default)
    {
        _context.AnalysisResults.Update(result);
        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
