using System.Security.Claims;

namespace NpgsqlRest.Auth;

public class NpgsqlRestAuthenticationOptions
{
    /// <summary>
    /// Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.
    ///
    /// This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`
    /// 
    /// If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.
    /// </summary>
    public string? DefaultAuthenticationType { get; set; } = null;

    /// <summary>
    /// The default column name to in the data reader which will be used to read the value to determine the success or failure of the login operation.
    /// 
    /// - If this column is not present, the success is when the endpoint returns any records.
    /// - If this column is not present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
    /// - If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.
    /// </summary>
    public string? StatusColumnName { get; set; } = "status";

    /// <summary>
    /// The default column name to in the data reader which will be used to read the value of the authentication scheme of the login process.
    /// 
    /// If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.
    /// </summary>
    public string? SchemeColumnName { get; set; } = "scheme";

    /// <summary>
    /// The default column name to in the data reader which will return a text message with the login status.
    /// </summary>
    public string? MessageColumnName { get; set; } = "message";

    /// <summary>
    /// Any columns retrieved from the reader during login, which don't have a name in `StatusColumnName` or `SchemeColumnName` will be used to create a new identity  `Claim`:
    /// 
    /// Column name will be interpreted as the claim type and the associated reader value for that column will be the claim value.
    /// 
    /// When this value is set to true (default) column name will try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value.
    /// 
    /// For example, column name `NameIdentifier` or `name_identifier` (when transformed by the default name transformer) will match the key `NameIdentifier` which translates to this: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
    /// </summary>
    public bool UseActiveDirectoryFederationServicesClaimTypes { get; set; } = true;

    /// <summary>
    /// Claim type value used to retrieve the user name. The user name is exposed as the default name with the `Name` property on the user identity.
    /// 
    /// The role key is used in the `bool IsInRole(string role)` method to search claims to determine does the current user identity belongs to roles.
    /// 
    /// The default is the Active Directory Federation Services Claim Type Role property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`(https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.role?view=net-8.0#system-security-claims-claimtypes-role)
    /// </summary>
    public string DefaultNameClaimType { get; set; } = ClaimTypes.Name;

    /// <summary>
    /// Claim type value used to define role-based security. 
    /// Default is the Active Directory Federation Services Claim Type Role: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    /// </summary>
    public string DefaultRoleClaimType { get; set; } = ClaimTypes.Role;

    /// <summary>
    /// If true, return any response from auth endpoints (login and logout) if response hasn't been written by auth handler.
    /// For cookie auth, this will return full record to response as returned by the routine.
    /// For bearer token auth, this will be ignored because bearer token auth writes it's own response (with tokens).
    /// This option will also be ignored if message column is present (see MessageColumnName option).
    /// Default is false.
    /// </summary>
    public bool SerializeAuthEndpointsResponse { get; set; } = false;

    /// <summary>
    /// Don't write real parameter values when logging parameters from auth endpoints and obfuscate instead.
    /// This prevents user credentials including password to end up in application logs.
    /// Default is true.
    /// </summary>
    public bool ObfuscateAuthParameterLogValues { get; set; } = true;

    /// <summary>
    /// The default column name to in the data reader which will be used to read the value of the hash of the password.
    /// If this column is present, the value will be used to verify the password from the password parameter.
    /// Password parameter is the first parameter which name contains the value of PasswordParameterNameContains.
    /// If verification fails, the login will fail and the HTTP Status Code will be set to 404 Not Found.
    /// </summary>
    public string HashColumnName { get; set; } = "hash";

    /// <summary>
    /// The default name of the password parameter. The first parameter which name contains this value will be used as the password parameter.
    /// This is used to verify the password from the password parameter when login endpoint returns a hash of the password (see HashColumnName).
    /// </summary>
    public string PasswordParameterNameContains { get; set; } = "pass";

    /// <summary>
    /// Default password hasher object. Inject custom password hasher object to add default password hasher.
    /// </summary>
    public IPasswordHasher? PasswordHasher { get; set; } = new PasswordHasher();

    /// <summary>
    /// Command that is executed when the password verification fails. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationFailedCommand { get; set; } = null;

    /// <summary>
    /// Command that is executed when the password verification succeeds. There are three text parameters:
    ///     - authentication scheme used for the login (if exists)
    ///     - user id used for the login (if exists)
    ///     - user name used for the login (if exists)
    /// Please use PostgreSQL parameter placeholders for the parameters ($1, $2, $3).
    /// </summary>
    public string? PasswordVerificationSucceededCommand { get; set; } = null;

    /// <summary>
    /// Set user context to true for all requests. 
    /// When this is set to true, user information (user id, user name and user roles) will be set to the context variables.
    /// You can set this individually for each request.
    /// </summary>
    public bool UseUserContext { get; set; } = false;

    /// <summary>
    /// User id context key that is used to set context variable for the user id.
    /// </summary>
    public string? UserIdContextKey { get; set; } = "request.user_id";

    /// <summary>
    /// User name context key that is used to set context variable for the user name.
    /// </summary>
    public string? UserNameContextKey { get; set; } = "request.user_name";

    /// <summary>
    /// User roles context key that is used to set context variable for the user roles.
    /// </summary>
    public string? UserRolesContextKey { get; set; } = "request.user_roles";
}
