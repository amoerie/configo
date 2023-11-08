using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed class ApplicationVariableRecord
{
    public int ApplicationId { get; set; }
    public int VariableId { get; set; }
}

public class ApplicationVariableRecordConfigurator: IEntityTypeConfiguration<ApplicationVariableRecord>
{
    public void Configure(EntityTypeBuilder<ApplicationVariableRecord> builder)
    {
        builder.HasKey(r => new { r.ApplicationId, r.VariableId });
        builder.HasOne<ApplicationRecord>().WithMany().HasForeignKey(r => r.ApplicationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VariableRecord>().WithMany().HasForeignKey(r => r.VariableId).OnDelete(DeleteBehavior.Cascade);
    }
}
