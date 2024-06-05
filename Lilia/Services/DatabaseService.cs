using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lilia.Services
{
    public class DatabaseService : DbContext
    {
        private readonly IServiceCollection serviceCollection;

        public DatabaseService(IServiceCollection serviceCollection)
        {
            this.serviceCollection = serviceCollection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source=lilia.sqlite;Password={}");
    }
}
