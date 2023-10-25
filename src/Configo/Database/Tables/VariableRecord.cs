using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed class VariableRecord
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
    public required DateTime ActiveFromUtc { get; set; }
}

public class VariableRecordConfigurator: IEntityTypeConfiguration<VariableRecord>
{
    public void Configure(EntityTypeBuilder<VariableRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Key).HasMaxLength(512);
    }
}
