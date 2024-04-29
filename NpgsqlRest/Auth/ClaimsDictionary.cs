using System.Collections.Frozen;

namespace NpgsqlRest.Auth;

public static class ClaimsDictionary
{
    //var names = string.Join($",{Environment.NewLine}", typeof(ClaimTypes)
    //  .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
    //  .Select(f => f.Name)
    //  .Order()
    //  .Select(n => $"{{ nameof(System.Security.Claims.ClaimTypes.{n}).ToLowerInvariant(), System.Security.Claims.ClaimTypes.{n} }}"));
    public static readonly FrozenDictionary<string, string> ClaimTypesDictionary = new Dictionary<string, string>()
    {
        { nameof(System.Security.Claims.ClaimTypes.Actor).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Actor },
        { nameof(System.Security.Claims.ClaimTypes.Anonymous).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Anonymous },
        { nameof(System.Security.Claims.ClaimTypes.Authentication).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Authentication },
        { nameof(System.Security.Claims.ClaimTypes.AuthenticationInstant).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthenticationInstant },
        { nameof(System.Security.Claims.ClaimTypes.AuthenticationMethod).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthenticationMethod },
        { nameof(System.Security.Claims.ClaimTypes.AuthorizationDecision).ToLowerInvariant(), System.Security.Claims.ClaimTypes.AuthorizationDecision },
        { nameof(System.Security.Claims.ClaimTypes.CookiePath).ToLowerInvariant(), System.Security.Claims.ClaimTypes.CookiePath },
        { nameof(System.Security.Claims.ClaimTypes.Country).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Country },
        { nameof(System.Security.Claims.ClaimTypes.DateOfBirth).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DateOfBirth },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyPrimaryGroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyPrimaryGroupSid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyPrimarySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyPrimarySid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlySid },
        { nameof(System.Security.Claims.ClaimTypes.DenyOnlyWindowsDeviceGroup).ToLowerInvariant(), System.Security.Claims.ClaimTypes.DenyOnlyWindowsDeviceGroup },
        { nameof(System.Security.Claims.ClaimTypes.Dns).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Dns },
        { nameof(System.Security.Claims.ClaimTypes.Dsa).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Dsa },
        { nameof(System.Security.Claims.ClaimTypes.Email).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Email },
        { nameof(System.Security.Claims.ClaimTypes.Expiration).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Expiration },
        { nameof(System.Security.Claims.ClaimTypes.Expired).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Expired },
        { nameof(System.Security.Claims.ClaimTypes.Gender).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Gender },
        { nameof(System.Security.Claims.ClaimTypes.GivenName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.GivenName },
        { nameof(System.Security.Claims.ClaimTypes.GroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.GroupSid },
        { nameof(System.Security.Claims.ClaimTypes.Hash).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Hash },
        { nameof(System.Security.Claims.ClaimTypes.HomePhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.HomePhone },
        { nameof(System.Security.Claims.ClaimTypes.IsPersistent).ToLowerInvariant(), System.Security.Claims.ClaimTypes.IsPersistent },
        { nameof(System.Security.Claims.ClaimTypes.Locality).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Locality },
        { nameof(System.Security.Claims.ClaimTypes.MobilePhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.MobilePhone },
        { nameof(System.Security.Claims.ClaimTypes.Name).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Name },
        { nameof(System.Security.Claims.ClaimTypes.NameIdentifier).ToLowerInvariant(), System.Security.Claims.ClaimTypes.NameIdentifier },
        { nameof(System.Security.Claims.ClaimTypes.OtherPhone).ToLowerInvariant(), System.Security.Claims.ClaimTypes.OtherPhone },
        { nameof(System.Security.Claims.ClaimTypes.PostalCode).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PostalCode },
        { nameof(System.Security.Claims.ClaimTypes.PrimaryGroupSid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PrimaryGroupSid },
        { nameof(System.Security.Claims.ClaimTypes.PrimarySid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.PrimarySid },
        { nameof(System.Security.Claims.ClaimTypes.Role).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Role },
        { nameof(System.Security.Claims.ClaimTypes.Rsa).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Rsa },
        { nameof(System.Security.Claims.ClaimTypes.SerialNumber).ToLowerInvariant(), System.Security.Claims.ClaimTypes.SerialNumber },
        { nameof(System.Security.Claims.ClaimTypes.Sid).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Sid },
        { nameof(System.Security.Claims.ClaimTypes.Spn).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Spn },
        { nameof(System.Security.Claims.ClaimTypes.StateOrProvince).ToLowerInvariant(), System.Security.Claims.ClaimTypes.StateOrProvince },
        { nameof(System.Security.Claims.ClaimTypes.StreetAddress).ToLowerInvariant(), System.Security.Claims.ClaimTypes.StreetAddress },
        { nameof(System.Security.Claims.ClaimTypes.Surname).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Surname },
        { nameof(System.Security.Claims.ClaimTypes.System).ToLowerInvariant(), System.Security.Claims.ClaimTypes.System },
        { nameof(System.Security.Claims.ClaimTypes.Thumbprint).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Thumbprint },
        { nameof(System.Security.Claims.ClaimTypes.Upn).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Upn },
        { nameof(System.Security.Claims.ClaimTypes.Uri).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Uri },
        { nameof(System.Security.Claims.ClaimTypes.UserData).ToLowerInvariant(), System.Security.Claims.ClaimTypes.UserData },
        { nameof(System.Security.Claims.ClaimTypes.Version).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Version },
        { nameof(System.Security.Claims.ClaimTypes.Webpage).ToLowerInvariant(), System.Security.Claims.ClaimTypes.Webpage },
        { nameof(System.Security.Claims.ClaimTypes.WindowsAccountName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsAccountName },
        { nameof(System.Security.Claims.ClaimTypes.WindowsDeviceClaim).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsDeviceClaim },
        { nameof(System.Security.Claims.ClaimTypes.WindowsDeviceGroup).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsDeviceGroup },
        { nameof(System.Security.Claims.ClaimTypes.WindowsFqbnVersion).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsFqbnVersion },
        { nameof(System.Security.Claims.ClaimTypes.WindowsSubAuthority).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsSubAuthority },
        { nameof(System.Security.Claims.ClaimTypes.WindowsUserClaim).ToLowerInvariant(), System.Security.Claims.ClaimTypes.WindowsUserClaim },
        { nameof(System.Security.Claims.ClaimTypes.X500DistinguishedName).ToLowerInvariant(), System.Security.Claims.ClaimTypes.X500DistinguishedName }
    }.ToFrozenDictionary();
}