using NPA.Core.Annotations;

namespace ProfilerDemo.Entities;

/// <summary>
/// Represents a user in the profiler demo.
/// Used to demonstrate performance profiling at scale (1M+ records).
/// </summary>
[Entity]
[Table("users")]
[NamedQuery("User.FindActiveUsersAsync",
            "SELECT u FROM User u WHERE u.IsActive = true",
            Description = "Finds all active users")]
[NamedQuery("User.FindByCountryAsync",
            "SELECT u FROM User u WHERE u.Country = :country ORDER BY u.Username",
            Description = "Finds users by country, ordered by username")]
[NamedQuery("User.FindHighBalanceUsersAsync",
            "SELECT u FROM User u WHERE u.AccountBalance > :minBalance ORDER BY u.AccountBalance DESC",
            Description = "Finds users with balance above threshold")]
[NamedQuery("User.FindRecentlyActiveAsync",
            "SELECT u FROM User u WHERE u.LastLogin >= :since ORDER BY u.LastLogin DESC",
            Description = "Finds users who logged in since a specific date")]
[NamedQuery("User.GetUserCountByCountry",
            "SELECT u.Country, COUNT(u) FROM User u GROUP BY u.Country",
            Description = "Gets user count grouped by country")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("age")]
    public int Age { get; set; }

    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("account_balance")]
    public decimal AccountBalance { get; set; }
}
