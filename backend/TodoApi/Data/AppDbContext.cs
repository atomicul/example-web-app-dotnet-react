using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Todos;

namespace TodoApi.Data;

/// <summary>
/// Identity store plus per-user data. Every <see cref="Todo"/> query is filtered to the
/// authenticated user and new todos are stamped with their owner on save.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Todo> Todos => Set<Todo>();

    private string? CurrentUserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Todo>(todo =>
        {
            todo.HasOne<IdentityUser>().WithMany().HasForeignKey(t => t.OwnerId);
            todo.HasQueryFilter("Owner", t => t.OwnerId == CurrentUserId);
        });
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Todo>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.OwnerId = CurrentUserId ?? throw new InvalidOperationException("Todos can only be created by an authenticated user.");
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
