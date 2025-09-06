using Microsoft.AspNetCore.Mvc;
using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using Brotal.FireflyBuddy.Models.ViewModels;

namespace Brotal.FireflyBuddy.Controllers.Mvc;

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
