using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportFlow.Infrastructure.Persistence;

public class SportFlowDbContextFactory : IDesignTimeDbContextFactory<SportFlowDbContext>
{
    public SportFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SportFlowDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=SportFlow_Dev;Username=postgres;Password=postgres",
            b => b.MigrationsAssembly("SportFlow.Infrastructure"));

        return new SportFlowDbContext(optionsBuilder.Options);
    }
}
