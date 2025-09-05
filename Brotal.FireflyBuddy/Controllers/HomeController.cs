using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;

namespace Brotal.FireflyBuddy.Controllers;

public class HomeController : Controller
{
    private readonly IIngestMessageRepository _messageRepository;
    private readonly ITransactionDraftRepository _draftRepository;

    public HomeController(IIngestMessageRepository messageRepository, ITransactionDraftRepository draftRepository)
    {
        _messageRepository = messageRepository;
        _draftRepository = draftRepository;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var messages = await _messageRepository.GetAllAsync(0, 10, cancellationToken);
        var drafts = await _draftRepository.GetAllAsync(0, 10, cancellationToken);
        
        var viewModel = new DashboardViewModel
        {
            RecentMessages = messages.ToList(),
            RecentDrafts = drafts.ToList()
        };
        
        return View(viewModel);
    }
}

public class MessagesController : Controller
{
    private readonly IIngestMessageRepository _messageRepository;

    public MessagesController(IIngestMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var messages = await _messageRepository.GetAllAsync(skip, pageSize, cancellationToken);
        
        var viewModel = new MessagesIndexViewModel
        {
            Messages = messages.ToList(),
            CurrentPage = page,
            PageSize = pageSize
        };
        
        return View(viewModel);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(id, cancellationToken);
        if (message == null)
            return NotFound();
            
        return View(message);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMessageViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var message = new IngestMessage
        {
            Text = model.Text,
            Source = model.Source ?? "Manual",
            Status = MessageStatus.Pending
        };
        
        await _messageRepository.CreateAsync(message, cancellationToken);
        
        return RedirectToAction(nameof(Details), new { id = message.Id });
    }

    public IActionResult Create()
    {
        return View(new CreateMessageViewModel());
    }
}

public class DraftsController : Controller
{
    private readonly ITransactionDraftRepository _draftRepository;
    private readonly FireflyClient _fireflyClient;

    public DraftsController(ITransactionDraftRepository draftRepository, FireflyClient fireflyClient)
    {
        _draftRepository = draftRepository;
        _fireflyClient = fireflyClient;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var drafts = await _draftRepository.GetAllAsync(skip, pageSize, cancellationToken);
        
        var viewModel = new DraftsIndexViewModel
        {
            Drafts = drafts.ToList(),
            CurrentPage = page,
            PageSize = pageSize
        };
        
        return View(viewModel);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft == null)
            return NotFound();
            
        return View(draft);
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft == null)
            return NotFound();
            
        var viewModel = new EditDraftViewModel
        {
            Id = draft.Id,
            Description = draft.Description,
            Amount = draft.Amount,
            Date = draft.Date,
            SourceAccountName = draft.SourceAccountName,
            DestinationAccountName = draft.DestinationAccountName,
            CategoryName = draft.CategoryName,
            BudgetName = draft.BudgetName,
            Notes = draft.Notes,
            Tags = draft.Tags,
            SubscriptionName = draft.SubscriptionName,
            ExternalUrl = draft.ExternalUrl
        };
        
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditDraftViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var draft = await _draftRepository.GetByIdAsync(model.Id, cancellationToken);
        if (draft == null)
            return NotFound();
            
        draft.Description = model.Description;
        draft.Amount = model.Amount;
        draft.Date = model.Date;
        draft.SourceAccountName = model.SourceAccountName;
        draft.DestinationAccountName = model.DestinationAccountName;
        draft.CategoryName = model.CategoryName;
        draft.BudgetName = model.BudgetName;
        draft.Notes = model.Notes;
        draft.Tags = model.Tags;
        draft.SubscriptionName = model.SubscriptionName;
        draft.ExternalUrl = model.ExternalUrl;
        draft.Status = DraftStatus.Ready;
        
        await _draftRepository.UpdateAsync(draft, cancellationToken);
        
        return RedirectToAction(nameof(Details), new { id = draft.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft == null)
            return NotFound();
            
        try
        {
            var fireflyDraft = new TransactionDraft
            {
                Type = draft.Type,
                Description = draft.Description,
                Date = draft.Date,
                Amount = draft.Amount,
                CurrencyCode = draft.CurrencyCode,
                SourceAccountName = draft.SourceAccountName,
                DestinationAccountName = draft.DestinationAccountName,
                CategoryName = draft.CategoryName,
                BudgetName = draft.BudgetName,
                Notes = draft.Notes,
                Tags = draft.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
                SubscriptionName = draft.SubscriptionName,
                ExternalUrl = draft.ExternalUrl
            };
            
            var transactionId = await _fireflyClient.CreateTransactionAsync(fireflyDraft, draft.IngestMessage.Text, cancellationToken);
            
            draft.Status = DraftStatus.Submitted;
            draft.SubmittedAt = DateTime.UtcNow;
            draft.FireflyTransactionId = transactionId;
            await _draftRepository.UpdateAsync(draft, cancellationToken);
            
            TempData["SuccessMessage"] = $"Transaction submitted successfully! Firefly ID: {transactionId}";
        }
        catch (Exception ex)
        {
            draft.Status = DraftStatus.Failed;
            draft.SubmissionError = ex.Message;
            await _draftRepository.UpdateAsync(draft, cancellationToken);
            
            TempData["ErrorMessage"] = $"Failed to submit transaction: {ex.Message}";
        }
        
        return RedirectToAction(nameof(Details), new { id = draft.Id });
    }
}

// View Models
public class DashboardViewModel
{
    public List<IngestMessage> RecentMessages { get; set; } = new();
    public List<TransactionDraft> RecentDrafts { get; set; } = new();
}

public class MessagesIndexViewModel
{
    public List<IngestMessage> Messages { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class CreateMessageViewModel
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Source { get; set; }
}

public class DraftsIndexViewModel
{
    public List<TransactionDraft> Drafts { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class EditDraftViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public DateTimeOffset Date { get; set; }
    
    [MaxLength(200)]
    public string SourceAccountName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string DestinationAccountName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string CategoryName { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string BudgetName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    [MaxLength(500)]
    public string? Tags { get; set; }
    
    [MaxLength(200)]
    public string? SubscriptionName { get; set; }
    
    [MaxLength(500)]
    public string? ExternalUrl { get; set; }
}
