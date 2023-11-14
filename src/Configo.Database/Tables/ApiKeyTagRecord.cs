using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configo.Database.Tables;

public sealed record ApiKeyTagRecord
{
    public int ApiKeyId { get; set; }
    public int TagId { get; set; }
}

public class TagApiKeyRecordConfigurator: IEntityTypeConfiguration<ApiKeyTagRecord>
{
    public void Configure(EntityTypeBuilder<ApiKeyTagRecord> builder)
    {
        builder.HasKey(r => new { r.ApiKeyId, r.TagId });
        builder.HasOne<ApiKeyRecord>().WithMany().HasForeignKey(r => r.ApiKeyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TagRecord>().WithMany().HasForeignKey(r => r.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
