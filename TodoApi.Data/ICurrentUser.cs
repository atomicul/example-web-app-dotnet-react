namespace TodoApi.Data;

/// <summary>Identifies the user the current operation runs on behalf of.</summary>
public interface ICurrentUser
{
    /// <summary>The user's identifier, or <c>null</c> when nobody is authenticated.</summary>
    string? Id { get; }
}
