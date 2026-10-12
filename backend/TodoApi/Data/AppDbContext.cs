using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Todos;

namespace TodoApi.Data;

/// <summary>
/// Identity store, the Data Protection key ring, plus per-user data. Every <see cref="IOwnedByUser"/> query is
/// filtered to the authenticated user and new owned rows are stamped with their owner on save.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    public DbSet<Todo> Todos => Set<Todo>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    private string? CurrentUserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var ownedTypes = builder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(type => type.IsAssignableTo(typeof(IOwnedByUser)))
            .ToList();

        foreach (var type in ownedTypes)
        {
            var entity = builder.Entity(type);
            entity.HasOne(typeof(IdentityUser)).WithMany().HasForeignKey(nameof(IOwnedByUser.OwnerId));
            entity.HasQueryFilter("Owner", OwnerFilter(type));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOwners();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampOwners();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Builds <c>e =&gt; e.OwnerId == CurrentUserId</c> for the given entity type.</summary>
    private LambdaExpression OwnerFilter(Type type)
    {
        var entity = Expression.Parameter(type, "e");
        var ownerId = Expression.Property(entity, nameof(IOwnedByUser.OwnerId));
        var currentUserId = Expression.Property(Expression.Constant(this), nameof(CurrentUserId));
        return Expression.Lambda(Expression.Equal(ownerId, currentUserId), entity);
    }

    private void StampOwners()
    {
        foreach (var entry in ChangeTracker.Entries<IOwnedByUser>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.OwnerId = CurrentUserId ?? throw new InvalidOperationException("Owned entities can only be created by an authenticated user.");
        }
    }
}
