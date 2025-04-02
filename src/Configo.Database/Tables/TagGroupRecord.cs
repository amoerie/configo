using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record TagGroupRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
    
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class TagGroupRecordConfigurator: IEntityTypeConfiguration<TagGroupRecord>
{
    public void Configure(EntityTypeBuilder<TagGroupRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.HasMany<TagRecord>()
            .WithOne()
            .HasForeignKey(v => v.TagGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
