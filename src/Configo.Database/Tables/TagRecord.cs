using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record TagRecord
{
    public int Id { get; set; }
    public required int TagGroupId { get; set; }
    public required string Name { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class TagRecordConfigurator: IEntityTypeConfiguration<TagRecord>
{
    public void Configure(EntityTypeBuilder<TagRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(256);
        builder.HasOne<TagGroupRecord>()
            .WithMany()
            .HasForeignKey(t => t.TagGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
