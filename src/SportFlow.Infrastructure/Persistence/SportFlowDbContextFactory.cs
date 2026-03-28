using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportFlow.Infrastructure.Persistence;

public class SportFlowDbContextFactory : IDesignTimeDbContextFactory<SportFlowDbContext>
{
    public SportFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SportFlowDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=SportFlow_Dev;Trusted_Connection=True;TrustServerCertificate=True;",
            b => b.MigrationsAssembly("SportFlow.Infrastructure"));

        return new SportFlowDbContext(optionsBuilder.Options);
    }
}
