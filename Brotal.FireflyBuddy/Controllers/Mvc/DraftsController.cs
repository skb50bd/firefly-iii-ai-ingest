using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Models.ViewModels;
using Brotal.FireflyBuddy.Models.AI;
using Brotal.FireflyBuddy.Services;

namespace Brotal.FireflyBuddy.Controllers.Mvc;

public class DraftsController : Controller
{
    private readonly ITransactionDraftRepository _draftRepository;
    private readonly IFireflyClient _fireflyClient;

    public DraftsController(ITransactionDraftRepository draftRepository, IFireflyClient fireflyClient)
    {
        _draftRepository = draftRepository;
        _fireflyClient   = fireflyClient;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var drafts = await _draftRepository.GetAllAsync(skip, pageSize, cancellationToken);

        var viewModel = new DraftsIndexViewModel
        {
            Drafts      = drafts.ToList(),
            CurrentPage = page,
            PageSize    = pageSize
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
            Id                     = draft.Id,
            Description            = draft.Description,
            Amount                 = draft.Amount,
            Date                   = draft.Date,
            SourceAccountName      = draft.SourceAccountName,
            DestinationAccountName = draft.DestinationAccountName,
            CategoryName           = draft.CategoryName,
            BudgetName             = draft.BudgetName,
            Notes                  = draft.Notes,
            Tags                   = draft.Tags,
            SubscriptionName       = draft.SubscriptionName,
            ExternalUrl            = draft.ExternalUrl
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

        draft.Description            = model.Description;
        draft.Amount                 = model.Amount;
        draft.Date                   = model.Date;
        draft.SourceAccountName      = model.SourceAccountName;
        draft.DestinationAccountName = model.DestinationAccountName;
        draft.CategoryName           = model.CategoryName;
        draft.BudgetName             = model.BudgetName;
        draft.Notes                  = model.Notes;
        draft.Tags                   = model.Tags;
        draft.SubscriptionName       = model.SubscriptionName;
        draft.ExternalUrl            = model.ExternalUrl;
        draft.Status                 = DraftStatus.Ready;

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
            var fireflyDraft = new FireflyTransactionDraft
            {
                Type                   = draft.Type,
                Description            = draft.Description,
                Date                   = draft.Date,
                Amount                 = draft.Amount,
                CurrencyCode           = draft.CurrencyCode,
                SourceAccountName      = draft.SourceAccountName,
                DestinationAccountName = draft.DestinationAccountName,
                CategoryName           = draft.CategoryName,
                BudgetName             = draft.BudgetName,
                Notes                  = draft.Notes,
                Tags                   = draft.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
                SubscriptionName       = draft.SubscriptionName,
                ExternalUrl            = draft.ExternalUrl
            };

            var transactionId = await _fireflyClient.CreateTransactionAsync(fireflyDraft, draft.IngestMessage.Text, cancellationToken);

            draft.Status               = DraftStatus.Submitted;
            draft.SubmittedAt          = DateTime.UtcNow;
            draft.FireflyTransactionId = transactionId;
            await _draftRepository.UpdateAsync(draft, cancellationToken);

            TempData["SuccessMessage"] = $"Transaction submitted successfully! Firefly ID: {transactionId}";
        }
        catch (Exception ex)
        {
            draft.Status          = DraftStatus.Failed;
            draft.SubmissionError = ex.Message;
            await _draftRepository.UpdateAsync(draft, cancellationToken);

            TempData["ErrorMessage"] = $"Failed to submit transaction: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id = draft.Id });
    }
}
