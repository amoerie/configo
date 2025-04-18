﻿using Configo.Database.Tables;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Configo.Database;

public class ConfigoDbContext : DbContext, IDataProtectionKeyContext
{
    public DbSet<ApiKeyRecord> ApiKeys => Set<ApiKeyRecord>();
    public DbSet<ApiKeyTagRecord> ApiKeyTags => Set<ApiKeyTagRecord>();
    public DbSet<ApplicationRecord> Applications => Set<ApplicationRecord>();
    public DbSet<VariableRecord> Variables => Set<VariableRecord>();
    public DbSet<TagGroupRecord> TagGroups => Set<TagGroupRecord>();
    public DbSet<TagRecord> Tags => Set<TagRecord>();
    public DbSet<UserRecord> Users => Set<UserRecord>();
    
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public ConfigoDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigoDbContext).Assembly);
    }
}
