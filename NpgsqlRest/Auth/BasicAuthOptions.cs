namespace NpgsqlRest.Auth;

public class EndpointBasicAuthOptions
{
    public bool Enabled { get; set; } = false;
    public string? Realm { get; set; } = null;
    public string? Username { get; set; } = null;
    public string? Password { get; set; } = null;
    public string? ChallengeCommand { get; set; } = null;
}

public class BasicAuthOptions : EndpointBasicAuthOptions
{
    public bool UseDefaultPasswordHasher { get; set; } = true;
    public Location PasswordHashLocation { get; set; } = Location.Server;
    public bool UseDefaultPasswordEncryptionOnClient { get; set; } = false;
    public bool UseDefaultPasswordEncryptionOnServer { get; set; } = false;
}