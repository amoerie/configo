using Configo.Database.Tables;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Configo.Database;

public class ConfigoDbContext : DbContext, IDataProtectionKeyContext
{
    public DbSet<ApplicationRecord> Applications => Set<ApplicationRecord>();
    public DbSet<VariableRecord> Variables => Set<VariableRecord>();
    public DbSet<TagRecord> Tags => Set<TagRecord>();
    public DbSet<TagVariableRecord> TagVariables => Set<TagVariableRecord>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public ConfigoDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigoDbContext).Assembly);
    }

}
