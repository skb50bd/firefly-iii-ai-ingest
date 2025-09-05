using Brotal.FireflyBuddy.Data;
using Brotal.FireflyBuddy.Repositories;
using TickerQ;

namespace Brotal.FireflyBuddy.Jobs;

public class MessageProcessingJobs(
    IIngestMessageRepository messageRepository,
    IAnalysisResultRepository analysisRepository,
    ITransactionDraftRepository draftRepository,
    AiService aiService,
    FireflyClient fireflyClient,
    ILogger<MessageProcessingJobs> logger)
{
    [TickerFunction(functionName: "ProcessPendingMessages", cronExpression: "*/5 * * * *")] // Every 5 minutes
    public async Task ProcessPendingMessages(TickerFunctionContext<string> tickerContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting to process pending messages");

        var pendingMessages = await messageRepository.GetByStatusAsync(MessageStatus.Pending, cancellationToken);
        
        foreach (var message in pendingMessages)
        {
            try
            {
                logger.LogInformation("Processing message {messageId}: {text}", message.Id, message.Text[..Math.Min(50, message.Text.Length)]);
                
                // Update status to processing
                message.Status = MessageStatus.Processing;
                await messageRepository.UpdateAsync(message, cancellationToken);

                // Get Firefly context
                var context = await fireflyClient.GetContextAsync(cancellationToken);
                
                // Analyze the message
                var analysis = await aiService.AnalyzeAsync(message.Text, context, cancellationToken);
                
                // Save analysis result
                var analysisResult = new AnalysisResult
                {
                    IngestMessageId = message.Id,
                    IsTransactional = analysis.IsTransactional,
                    IsConfident = analysis.IsConfident,
                    Reason = analysis.Reason,
                    CreatedAt = DateTime.UtcNow
                };
                
                await analysisRepository.CreateAsync(analysisResult, cancellationToken);

                // If transactional and confident, create draft
                if (analysis.IsTransactional && analysis.IsConfident && analysis.Draft is not null)
                {
                    var draft = new TransactionDraft
                    {
                        IngestMessageId = message.Id,
                        Type = analysis.Draft.Type,
                        Description = analysis.Draft.Description,
                        Date = analysis.Draft.Date,
                        Amount = analysis.Draft.Amount,
                        CurrencyCode = analysis.Draft.CurrencyCode,
                        SourceAccountName = analysis.Draft.SourceAccountName,
                        DestinationAccountName = analysis.Draft.DestinationAccountName,
                        CategoryName = analysis.Draft.CategoryName,
                        BudgetName = analysis.Draft.BudgetName,
                        Notes = analysis.Draft.Notes,
                        Tags = analysis.Draft.Tags != null ? string.Join(",", analysis.Draft.Tags) : null,
                        SubscriptionName = analysis.Draft.SubscriptionName,
                        ExternalUrl = analysis.Draft.ExternalUrl,
                        Status = DraftStatus.Ready,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await draftRepository.CreateAsync(draft, cancellationToken);
                    logger.LogInformation("Created transaction draft {draftId} for message {messageId}", draft.Id, message.Id);
                }

                // Mark message as processed
                message.Status = MessageStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                await messageRepository.UpdateAsync(message, cancellationToken);
                
                logger.LogInformation("Successfully processed message {messageId}", message.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message {messageId}", message.Id);
                
                message.Status = MessageStatus.Failed;
                message.ProcessingError = ex.Message;
                await messageRepository.UpdateAsync(message, cancellationToken);
            }
        }

        logger.LogInformation("Finished processing pending messages. Processed {count} messages", pendingMessages.Count());
    }

    [TickerFunction(functionName: "SubmitReadyDrafts", cronExpression: "*/10 * * * *")] // Every 10 minutes
    public async Task SubmitReadyDrafts(TickerFunctionContext<string> tickerContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting to submit ready drafts");

        var readyDrafts = await draftRepository.GetByStatusAsync(DraftStatus.Ready, cancellationToken);
        
        foreach (var draft in readyDrafts)
        {
            try
            {
                logger.LogInformation("Submitting draft {draftId}: {description}", draft.Id, draft.Description);
                
                // Convert draft to Firefly transaction format
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

                // Submit to Firefly-III
                var transactionId = await fireflyClient.CreateTransactionAsync(fireflyDraft, draft.IngestMessage.Text, cancellationToken);
                
                // Update draft status
                draft.Status = DraftStatus.Submitted;
                draft.SubmittedAt = DateTime.UtcNow;
                draft.FireflyTransactionId = transactionId;
                await draftRepository.UpdateAsync(draft, cancellationToken);
                
                logger.LogInformation("Successfully submitted draft {draftId} as transaction {transactionId}", draft.Id, transactionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error submitting draft {draftId}", draft.Id);
                
                draft.Status = DraftStatus.Failed;
                draft.SubmissionError = ex.Message;
                await draftRepository.UpdateAsync(draft, cancellationToken);
            }
        }

        logger.LogInformation("Finished submitting ready drafts. Processed {count} drafts", readyDrafts.Count());
    }
}
