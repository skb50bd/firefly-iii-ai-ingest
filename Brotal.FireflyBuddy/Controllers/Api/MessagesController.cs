using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Models.Requests;
using Brotal.FireflyBuddy.Models.Responses;

namespace Brotal.FireflyBuddy.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IIngestMessageRepository _messageRepository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IIngestMessageRepository messageRepository, ILogger<MessagesController> logger)
    {
        _messageRepository = messageRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new message for ingestion
    /// </summary>
    /// <param name="request">The message data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created message ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(MessageCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMessage(
        [FromBody] IngestMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var message = new IngestMessage
        {
            Text       = request.Text,
            Source     = request.Source ?? "API",
            ExternalId = request.ExternalId,
            Status     = MessageStatus.Pending
        };

        var createdMessage = await _messageRepository.CreateAsync(message, cancellationToken);
        _logger.LogInformation("Created message {MessageId} for ingestion", createdMessage.Id);

        return CreatedAtAction(
            nameof(GetMessage),
            new { id = createdMessage.Id },
            new MessageCreatedResponse(createdMessage.Id));
    }

    /// <summary>
    /// Gets all messages with pagination
    /// </summary>
    /// <param name="skip">Number of messages to skip</param>
    /// <param name="take">Number of messages to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of messages</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IngestMessage>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await _messageRepository.GetAllAsync(skip, take, cancellationToken);
        return Ok(messages);
    }

    /// <summary>
    /// Gets a specific message by ID
    /// </summary>
    /// <param name="id">The message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message or 404 if not found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IngestMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessage(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await _messageRepository.GetByIdAsync(id, cancellationToken);
        return message is not null ? Ok(message) : NotFound();
    }
}
