using System.Security.Claims;

namespace NpgsqlRest;

public class NpgsqlRestAuthenticationOptions(
    string? defaultAuthenticationType = null,
    string? statusColumnName = "status",
    string? schemaColumnName = "schema",
    string? messageColumnName = "message",
    bool useActiveDirectoryFederationServicesClaimTypes = true,
    string defaultNameClaimType = ClaimTypes.Name,
    string defaultRoleClaimType = ClaimTypes.Role)
{
    /// <summary>
    /// Authentication type used with the Login endpoints to set the authentication type for the new `ClaimsIdentity` created by the login.
    ///
    /// This value must be set to non-null when using login endpoints, otherwise, the following error will raise: `SignInAsync when principal.Identity.IsAuthenticated is false is not allowed when AuthenticationOptions.RequireAuthenticatedSignIn is true.`
    /// 
    /// If the value is not set and the login endpoint is present, it will automatically get the database name from the connection string.
    /// </summary>
    public string? DefaultAuthenticationType { get; set; } = defaultAuthenticationType;

    /// <summary>
    /// The default column name to in the data reader which will be used to read the value to determine the success or failure of the login operation.
    /// 
    /// - If this column is not present, the success is when the endpoint returns any records.
    /// - If this column is not present, it must be either a boolean to indicate success or a numeric value to indicate the HTTP Status Code to return.
    /// - If this column is present and retrieves a numeric value, that value is assigned to the HTTP Status Code and the login will authenticate only when this value is 200.
    /// </summary>
    public string? StatusColumnName { get; set; } = statusColumnName;

    /// <summary>
    /// The default column name to in the data reader which will be used to read the value of the authentication scheme of the login process.
    /// 
    /// If this column is not present in the login response the default authentication scheme is used. Return new value to use a different authentication scheme with the login endpoint.
    /// </summary>
    public string? SchemaColumnName { get; set; } = schemaColumnName;

    /// <summary>
    /// The default column name to in the data reader which will return a text message with the login status.
    /// </summary>
    public string? MessageColumnName { get; set; } = messageColumnName;

    /// <summary>
    /// Any columns retrieved from the reader during login, which don't have a name in `StatusColumnName` or `SchemeColumnName` will be used to create a new identity  `Claim`:
    /// 
    /// Column name will be interpreted as the claim type and the associated reader value for that column will be the claim value.
    /// 
    /// When this value is set to true (default) column name will try to match the constant name in the [ClaimTypes class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes?view=net-8.0) to retrieve the value.
    /// 
    /// For example, column name `NameIdentifier` or `name_identifier` (when transformed by the default name transformer) will match the key `NameIdentifier` which translates to this: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
    /// </summary>
    public bool UseActiveDirectoryFederationServicesClaimTypes { get; set; } = useActiveDirectoryFederationServicesClaimTypes;

    /// <summary>
    /// Claim type value used to retrieve the user name. The user name is exposed as the default name with the `Name` property on the user identity.
    /// 
    /// The role key is used in the `bool IsInRole(string role)` method to search claims to determine does the current user identity belongs to roles.
    /// 
    /// The default is the Active Directory Federation Services Claim Type Role property with value [`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`(https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.role?view=net-8.0#system-security-claims-claimtypes-role)
    /// </summary>
    public string DefaultNameClaimType { get; set; } = defaultNameClaimType;

    /// <summary>
    /// Claim type value used to define role-based security. 
    /// Default is the Active Directory Federation Services Claim Type Role: http://schemas.microsoft.com/ws/2008/06/identity/claims/role
    /// </summary>
    public string DefaultRoleClaimType { get; set; } = defaultRoleClaimType;
}
