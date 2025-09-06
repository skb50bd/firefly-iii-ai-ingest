using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Models.Requests;
using Brotal.FireflyBuddy.Models.Responses;
using Brotal.FireflyBuddy.Models.AI;
using Brotal.FireflyBuddy.Services;

namespace Brotal.FireflyBuddy.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class DraftsController : ControllerBase
{
    private readonly ITransactionDraftRepository _draftRepository;
    private readonly IFireflyClient _fireflyClient;
    private readonly ILogger<DraftsController> _logger;

    public DraftsController(
        ITransactionDraftRepository draftRepository,
        IFireflyClient fireflyClient,
        ILogger<DraftsController> logger)
    {
        _draftRepository = draftRepository;
        _fireflyClient   = fireflyClient;
        _logger          = logger;
    }

    /// <summary>
    /// Gets all transaction drafts with pagination
    /// </summary>
    /// <param name="skip">Number of drafts to skip</param>
    /// <param name="take">Number of drafts to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transaction drafts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TransactionDraft>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDrafts(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var drafts = await _draftRepository.GetAllAsync(skip, take, cancellationToken);
        return Ok(drafts);
    }

    /// <summary>
    /// Gets a specific transaction draft by ID
    /// </summary>
    /// <param name="id">The draft ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The draft or 404 if not found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDraft), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDraft(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        return draft is not null ? Ok(draft) : NotFound();
    }

    /// <summary>
    /// Updates a transaction draft
    /// </summary>
    /// <param name="id">The draft ID</param>
    /// <param name="request">The update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated draft or 404 if not found</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDraft), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDraft(
        Guid id,
        [FromBody] UpdateDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft is null)
            return NotFound();

        // Update draft properties
        draft.Description            = request.Description ?? draft.Description;
        draft.Amount                 = request.Amount ?? draft.Amount;
        draft.Date                   = request.Date ?? draft.Date;
        draft.SourceAccountName      = request.SourceAccountName ?? draft.SourceAccountName;
        draft.DestinationAccountName = request.DestinationAccountName ?? draft.DestinationAccountName;
        draft.CategoryName           = request.CategoryName ?? draft.CategoryName;
        draft.BudgetName             = request.BudgetName ?? draft.BudgetName;
        draft.Notes                  = request.Notes ?? draft.Notes;
        draft.Tags                   = request.Tags ?? draft.Tags;
        draft.Status                 = DraftStatus.Ready; // Mark as ready for submission

        var updatedDraft = await _draftRepository.UpdateAsync(draft, cancellationToken);
        return Ok(updatedDraft);
    }

    /// <summary>
    /// Submits a transaction draft to Firefly-III
    /// </summary>
    /// <param name="id">The draft ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The Firefly transaction ID or error details</returns>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(TransactionSubmittedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitDraft(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var draft = await _draftRepository.GetByIdAsync(id, cancellationToken);
        if (draft is null)
            return NotFound();

        try
        {
            // Convert to Firefly format
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

            _logger.LogInformation("Submitted draft {DraftId} as transaction {TransactionId}", draft.Id, transactionId);
            return Ok(new TransactionSubmittedResponse(transactionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting draft {DraftId}", draft.Id);
            draft.Status          = DraftStatus.Failed;
            draft.SubmissionError = ex.Message;
            await _draftRepository.UpdateAsync(draft, cancellationToken);
            return BadRequest(new { error = ex.Message });
        }
    }
}
