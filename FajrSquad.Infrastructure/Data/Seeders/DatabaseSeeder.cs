using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Data.Seeders
{
    public class DatabaseSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FajrDbContext>();

            try
            {
                _logger.LogInformation("Starting database seeding...");

                // Ensure database is created and migrated
                await context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Database migration completed");

                // Seed content
                var contentLogger = scope.ServiceProvider.GetRequiredService<ILogger<ContentSeeder>>();
                var contentSeeder = new ContentSeeder(context, contentLogger);
                await contentSeeder.SeedAsync();

                // Seed test users (only in Development)
                var environment = _serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    var testUserLogger = scope.ServiceProvider.GetRequiredService<ILogger<TestUserSeeder>>();
                    var testUserSeeder = new TestUserSeeder(context, testUserLogger);
                    await testUserSeeder.SeedMultipleTestUsersAsync();
                }

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
