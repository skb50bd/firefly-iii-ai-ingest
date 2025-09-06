using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Models.ViewModels;
using Brotal.FireflyBuddy.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Brotal.FireflyBuddy.Controllers.Mvc;

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
