using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record VariableRecord
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required VariableValueType ValueType { get; set; }
    public required int? TagId { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}

public class VariableRecordConfigurator: IEntityTypeConfiguration<VariableRecord>
{
    public void Configure(EntityTypeBuilder<VariableRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Key).HasMaxLength(512);
        builder.Property(r => r.ValueType).HasConversion<string>().HasMaxLength(16).HasDefaultValue(VariableValueType.String);
        builder.Property(r => r.CreatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.Property(r => r.UpdatedAtUtc).HasDefaultValue(DateTime.UnixEpoch);
        builder.HasOne<TagRecord>()
            .WithMany()
            .HasForeignKey(t => t.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
