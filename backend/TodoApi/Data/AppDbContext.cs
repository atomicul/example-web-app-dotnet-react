using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Todos;

namespace TodoApi.Data;

/// <summary>
/// Identity store plus per-user data. Every <see cref="Todo"/> query is filtered to the
/// authenticated user and new todos are stamped with their owner on save.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Todo>(todo =>
        {
            todo.HasOne<IdentityUser>().WithMany().HasForeignKey(t => t.OwnerId);
            todo.HasQueryFilter("Owner", t => t.OwnerId == currentUser.Id);
        });
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Todo>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.OwnerId = currentUser.Id ?? throw new InvalidOperationException("Todos can only be created by an authenticated user.");
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
