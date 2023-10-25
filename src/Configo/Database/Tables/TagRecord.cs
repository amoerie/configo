using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed class TagRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class TagRecordConfigurator: IEntityTypeConfiguration<TagRecord>
{
    public void Configure(EntityTypeBuilder<TagRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
    }
}
