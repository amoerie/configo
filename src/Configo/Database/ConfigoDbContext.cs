using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Database;

public class ConfigoDbContext : DbContext
{
    public DbSet<VariableRecord> Variables => Set<VariableRecord>();
    public DbSet<TagRecord> Tags => Set<TagRecord>();
    public DbSet<TagVariableRecord> TagVariables => Set<TagVariableRecord>();

    public ConfigoDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigoDbContext).Assembly);
    }
}
