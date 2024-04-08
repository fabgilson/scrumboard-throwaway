using System.Threading.Tasks;
using IdentityProvider.DataAccess;
using IdentityProvider.Services.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace IdentityProvider
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>().CreateDbContextAsync();
                if (db.Database.IsRelational())
                {
                    await db.Database.MigrateAsync();
                }
                else if (await db.Database.GetService<IDatabaseCreator>().CanConnectAsync())
                {
                    await db.Database.EnsureCreatedAsync();
                }

                var seedUserService = scope.ServiceProvider.GetService<ISeedUserService>();
                if (seedUserService is not null) await seedUserService.AddTestUsersAsync();
            }

            await webHost.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
