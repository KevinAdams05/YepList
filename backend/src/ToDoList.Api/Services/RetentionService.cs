using ToDoList.Data.Repositories;

namespace ToDoList.Api.Services
{
    /// <summary>
    /// Daily maintenance: purges sync-log rows and long-since soft-deleted
    /// entities older than the configured retention window so the database and
    /// audit trail don't grow without bound.
    ///
    /// Config (appsettings):
    ///   Retention:Enabled  (bool, default true)
    ///   Retention:Days     (int,  default 90)
    /// </summary>
    public class RetentionService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private readonly ILogger<RetentionService> logger;

        public RetentionService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RetentionService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using PeriodicTimer timer = new(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            if (!configuration.GetValue("Retention:Enabled", true))
            {
                return;
            }

            int days = configuration.GetValue("Retention:Days", 90);
            if (days <= 0)
            {
                logger.LogWarning("Retention:Days is {Days}; skipping purge to avoid deleting everything.", days);
                return;
            }

            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                IServiceProvider sp = scope.ServiceProvider;

                int logs = await sp.GetRequiredService<SyncLogRepository>().PurgeOlderThanAsync(days);
                int items = await sp.GetRequiredService<TodoItemRepository>().PurgeDeletedOlderThanAsync(days);
                int lists = await sp.GetRequiredService<TodoListRepository>().PurgeDeletedOlderThanAsync(days);
                int cats = await sp.GetRequiredService<CategoryRepository>().PurgeDeletedOlderThanAsync(days);
                await sp.GetRequiredService<DeletedEntityRepository>().PurgeOlderThanAsync(days);

                logger.LogInformation(
                    "Retention purge (>{Days}d): {Logs} log rows, {Items} items, {Lists} lists, {Cats} categories.",
                    days, logs, items, lists, cats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retention purge failed.");
            }
        }
    }
}
