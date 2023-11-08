using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record TagVariableRecord
{
    public int TagId { get; set; }
    public int VariableId { get; set; }
}

public class TagVariableRecordConfigurator: IEntityTypeConfiguration<TagVariableRecord>
{
    public void Configure(EntityTypeBuilder<TagVariableRecord> builder)
    {
        builder.HasKey(r => new { r.TagId, r.VariableId });
        builder.HasOne<TagRecord>().WithMany().HasForeignKey(r => r.TagId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VariableRecord>().WithMany().HasForeignKey(r => r.VariableId).OnDelete(DeleteBehavior.Cascade);
    }
}
