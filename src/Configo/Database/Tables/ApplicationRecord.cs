using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed class ApplicationRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string JsonSchema { get; set; }
}

public class ApplicationRecordConfigurator: IEntityTypeConfiguration<ApplicationRecord>
{
    public void Configure(EntityTypeBuilder<ApplicationRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
    }
}
