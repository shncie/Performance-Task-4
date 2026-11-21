using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Data;

public class CareFlowDbContextFactory : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CareFlowDbContext>();
        optionsBuilder.UseSqlite("Data Source=careflow.db");
        return new CareFlowDbContext(optionsBuilder.Options);
    }
}