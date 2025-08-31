using Brotal.FireflyIII.Api;
using Brotal.FireflyIII.Model;

namespace Brotal.FireflyBuddy;

public sealed class FireflyClient : IDisposable, IAsyncDisposable
{
    private DateTime _cacheExpiry = DateTime.UtcNow.AddHours(1);
    private readonly Lock _cacheLock = new();
    private readonly Timer _cacheExpiryTimer;
    private FireflyContextSnapshot? _cachedContext;
    private readonly ITransactionsApi _transactionsApi;
    private readonly ICategoriesApi _categoriesApi;
    private readonly ITagsApi _tagsApi;
    private readonly IAccountsApi _accountsApi;
    private readonly IBudgetsApi _budgetsApi;
    private readonly ISearchApi _searchApi;
    private readonly IRulesApi _rulesApi;
    private readonly IBillsApi _billsApi;
    private readonly ILogger<FireflyClient> _logger;

    public FireflyClient(
        ITransactionsApi transactionsApi,
        ICategoriesApi categoriesApi,
        ITagsApi tagsApi,
        IAccountsApi accountsApi,
        IBudgetsApi budgetsApi,
        ISearchApi searchApi,
        IRulesApi rulesApi,
        IBillsApi billsApi,
        ILogger<FireflyClient> logger
    )
    {
        // Set up hourly cache expiry timer
        _cacheExpiryTimer = new Timer(
            _ => InvalidateCache(),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1)
        );

        _transactionsApi = transactionsApi;
        _categoriesApi   = categoriesApi;
        _tagsApi         = tagsApi;
        _accountsApi     = accountsApi;
        _budgetsApi      = budgetsApi;
        _searchApi       = searchApi;
        _rulesApi        = rulesApi;
        _billsApi        = billsApi;
        _logger          = logger;
    }

    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedContext = null;
            _cacheExpiry   = DateTime.UtcNow;
            _logger.LogInformation("Invalidated cache");
        }
    }

    public async Task<FireflyContextSnapshot> FetchContextFromApiAsync(CancellationToken cancellationToken)
    {
        var assetAccountsTask       = _accountsApi.ListAccountAsync(limit:500, type: AccountTypeFilter.Asset, cancellationToken: cancellationToken);
        var expenseAccountsTask     = _accountsApi.ListAccountAsync(limit:500, type: AccountTypeFilter.Expense, cancellationToken: cancellationToken);
        var liabilitiesAccountsTask = _accountsApi.ListAccountAsync(limit:500, type: AccountTypeFilter.Liabilities, cancellationToken: cancellationToken);
        var revenueAccountsTask     = _accountsApi.ListAccountAsync(limit:500, type: AccountTypeFilter.Revenue, cancellationToken: cancellationToken);
        var categoriesTask          = _categoriesApi.ListCategoryAsync(cancellationToken: cancellationToken);
        var tagsTask                = _tagsApi.ListTagAsync(cancellationToken: cancellationToken);
        var budgetsTask             = _budgetsApi.ListBudgetAsync(cancellationToken: cancellationToken);
        var rulesTask               = _rulesApi.ListRuleAsync(cancellationToken: cancellationToken);
        var billsTask               = _billsApi.ListBillAsync(cancellationToken: cancellationToken);

        await Task.WhenAll(
            assetAccountsTask,
            expenseAccountsTask,
            liabilitiesAccountsTask,
            revenueAccountsTask,
            categoriesTask,
            tagsTask,
            budgetsTask,
            rulesTask,
            billsTask
        );

        _logger.LogInformation("Fetched context from Firefly III API");

        var assetAccountsResponse       = assetAccountsTask.Result;
        var expenseAccountsResponse     = expenseAccountsTask.Result;
        var liabilitiesAccountsResponse = liabilitiesAccountsTask.Result;
        var revenueAccountsResponse     = revenueAccountsTask.Result;
        var categoriesResponse          = categoriesTask.Result;
        var tagsResponse                = tagsTask.Result;
        var budgetsResponse             = budgetsTask.Result;
        var rulesResponse               = rulesTask.Result;
        var billsResponse               = billsTask.Result;

        if ((
                assetAccountsResponse.TryOk(out var assets)             && assets is not null &&
                expenseAccountsResponse.TryOk(out var expenses)         && expenses is not null &&
                liabilitiesAccountsResponse.TryOk(out var liabilities)  && liabilities is not null &&
                revenueAccountsResponse.TryOk(out var revenues)         && revenues is not null &&
                categoriesResponse.TryOk(out var categories)            && categories is not null &&
                tagsResponse.TryOk(out var tags)                        && tags is not null &&
                budgetsResponse.TryOk(out var budgets)                  && budgets is not null &&
                rulesResponse.TryOk(out var rules)                      && rules is not null &&
                billsResponse.TryOk(out var bills)                      && bills is not null
            ) is false
        )
        {
            _logger.LogError("Failed to fetch context from Firefly III API");
            throw new Exception("Failed to fetch context from Firefly III API");
        }

        return new FireflyContextSnapshot
        {
            AssetAccounts     = assets.Data,
            ExpenseAccounts   = expenses.Data,
            LiabilityAccounts = liabilities.Data,
            RevenueAccounts   = revenues.Data,
            Categories        = categories.Data,
            Tags              = tags.Data,
            Budgets           = budgets.Data,
            Subscriptions     = bills.Data
        };
    }

    public async Task<FireflyContextSnapshot> GetContextAsync(CancellationToken cancellationToken)
    {
        lock (_cacheLock)
        {
            if (_cachedContext != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedContext;
            }
        }

        var context = await FetchContextFromApiAsync(cancellationToken);

        lock (_cacheLock)
        {
            _cachedContext = context;
            _cacheExpiry = DateTime.UtcNow.AddHours(1);
            _logger.LogInformation("Cached context");
        }

        return context;
    }

    public async Task<string> CreateTransactionAsync(
        TransactionDraft draft,
        string originalMessage,
        CancellationToken cancellationToken
    )
    {
        var transaction = new TransactionStore(
            transactions: [
                new(
                    type:               draft.Type,
                    date:               draft.Date.UtcDateTime,
                    amount:             draft.Amount.ToString("#.00"),
                    description:        draft.Description,
                    budgetName:         draft.BudgetName,
                    currencyCode:       draft.CurrencyCode,
                    categoryName:       draft.CategoryName,
                    sourceName:         draft.SourceAccountName,
                    destinationName:    draft.DestinationAccountName,
                    tags:               draft.Tags.ToList(),
                    billName:           draft.SubscriptionName,
                    externalUrl:        draft.ExternalUrl,
                    notes:              string.IsNullOrWhiteSpace(draft.Notes)
                                            ? $"Ingested from message: {originalMessage}"
                                            : $"{draft.Notes}\n\nIngested from message: {originalMessage}"
                )
            ],
            errorIfDuplicateHash:   true,
            applyRules:             true
        );

        var response = await _transactionsApi.StoreTransactionAsync(transaction, cancellationToken: cancellationToken);

        if (response.TryOk(out var createdTransaction) && createdTransaction is not null)
        {
            _logger.LogInformation("Created transaction {transactionId}", createdTransaction.Data.Id);
            return createdTransaction.Data.Id;
        }

        _logger.LogError("Failed to create transaction in Firefly III");

        throw new Exception("Failed to create transaction in Firefly III");
    }

    public void Dispose()
    {
        _cacheExpiryTimer.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _cacheExpiryTimer.DisposeAsync();
    }
}