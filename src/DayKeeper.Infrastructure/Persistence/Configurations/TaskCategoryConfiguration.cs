using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DayKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="TaskCategory"/> join entity.
/// Defines foreign key constraints, Task and Category relationships,
/// and a unique composite index on <c>(TaskItemId, CategoryId)</c>.
/// </summary>
public sealed class TaskCategoryConfiguration : BaseEntityConfiguration<TaskCategory>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TaskCategory> builder)
    {
        builder.Property(e => e.TaskItemId)
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.HasOne(e => e.TaskItem)
            .WithMany(t => t.TaskCategories)
            .HasForeignKey(e => e.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.TaskCategories)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TaskItemId, e.CategoryId })
            .IsUnique();

        builder.HasIndex(e => e.CategoryId);
    }
}
