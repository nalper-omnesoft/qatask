using Microsoft.EntityFrameworkCore;
using ApiService.Models;

namespace ApiService.Data;

public class QaTaskDbContext : DbContext
{
    public QaTaskDbContext(DbContextOptions<QaTaskDbContext> options) : base(options) { }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use snake_case naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
        }

        modelBuilder.Entity<TodoItem>().HasData(
            new TodoItem { Id = 1, Title = "Review the application", IsComplete = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new TodoItem { Id = 2, Title = "Write Playwright tests", IsComplete = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new TodoItem { Id = 3, Title = "Submit test results", IsComplete = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }

    private static string ToSnakeCase(string name) =>
        string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
}
