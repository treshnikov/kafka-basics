using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MediatR;

public class BooksOutboxPublisher : INotificationHandler<NewMessageWasAddedIntoOutboxNotification>
{
    private readonly IBooksDbContext _dbContext;
    private readonly IKafkaBooksProducer _kafkaBooksProducer;
    private readonly ILogger<BooksOutboxPublisher> _logger;

    public BooksOutboxPublisher(IBooksDbContext dbContext, IKafkaBooksProducer kafkaBooksProducer, ILogger<BooksOutboxPublisher> logger)
    {
        _dbContext = dbContext;
        _kafkaBooksProducer = kafkaBooksProducer;
        _logger = logger;
    }

    public async Task Handle(NewMessageWasAddedIntoOutboxNotification notification, CancellationToken stopToken)
    {
        try
        {
            var outbox = _dbContext.BooksOutbox.ToArray();
            foreach (var book in outbox)
            {
                await _kafkaBooksProducer.ProduceAsync(book.Data, stopToken);
                //_logger.LogInformation($"{book.Data} has been published from outbox");
                _dbContext.BooksOutbox.Remove(book);
            }
            await _dbContext.SaveChangesAsync(stopToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Producer stopped.");
            throw;
        }
    }
}