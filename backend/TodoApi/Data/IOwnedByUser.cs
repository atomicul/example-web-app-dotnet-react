namespace TodoApi.Data;

/// <summary>
/// Marks an entity as belonging to a user. <see cref="AppDbContext"/> links it to its owner,
/// filters every query to the authenticated user and stamps the owner on new rows.
/// </summary>
public interface IOwnedByUser
{
    string OwnerId { get; set; }
}
