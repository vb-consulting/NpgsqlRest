using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Primitives;
using NpgsqlRest;

namespace NpgsqlRestClient;

public class DefaultResponseParser(
    string? userIdParameterName,
    string? userNameParameterName,
    string? userRolesParameterName,
    string? ipAddressParameterName,
    string? antiforgeryFieldNameTag,
    string? antiforgeryTokenTag,
    Dictionary<string, StringValues>? customClaims,
    Dictionary<string, string?>? customParameters) : IResponseParser
{
    private readonly string? userIdParameterName = userIdParameterName;
    private readonly string? userNameParameterName = userNameParameterName;
    private readonly string? userRolesParameterName = userRolesParameterName;
    private readonly string? ipAddressParameterName = ipAddressParameterName;

    private readonly string? antiforgeryFieldNameTag = antiforgeryFieldNameTag;
    private readonly string? antiforgeryTokenTag = antiforgeryTokenTag;

    private readonly Dictionary<string, StringValues>? customClaims = customClaims;
    private readonly Dictionary<string, string?>? customParameters = customParameters;

    public ReadOnlySpan<char> Parse(ReadOnlySpan<char> input, RoutineEndpoint endpoint, HttpContext context)
    {
        return Parse(input, context, null);
    }

    public ReadOnlySpan<char> Parse(ReadOnlySpan<char> input, HttpContext context, AntiforgeryTokenSet? tokenSet)
    {
        Dictionary<string, string> replacements = [];

        if (userIdParameterName is not null)
        {
            var value = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            replacements.Add(userIdParameterName, value is null ? Consts.Null : string.Concat(Consts.DoubleQuote, value, Consts.DoubleQuote)); 
        }
        if (userNameParameterName is not null)
        {
            var value = context.User.Identity?.Name;
            replacements.Add(userNameParameterName, value is null ? Consts.Null : string.Concat(Consts.DoubleQuote, value, Consts.DoubleQuote));
        }
        if (userRolesParameterName is not null)
        {
            var value = context.User.FindAll(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))?.Select(r => string.Concat(Consts.DoubleQuote, r.Value, Consts.DoubleQuote));
            replacements.Add(userRolesParameterName, value is null ? Consts.Null : string.Concat(Consts.OpenBracket, string.Join(Consts.Comma, value), Consts.CloseBracket));
        }
        if (ipAddressParameterName is not null)
        {
            var value = App.GetClientIpAddress(context.Request);
            replacements.Add(ipAddressParameterName, value is null ? Consts.Null : string.Concat(Consts.DoubleQuote, value, Consts.DoubleQuote));
        }
        if (customClaims is not null)
        {
            foreach (var (key, value) in customClaims)
            {
                var claim = context.User.FindFirst(key);
                replacements.Add(key, claim is null ? Consts.Null : string.Concat(Consts.DoubleQuote, claim.Value, Consts.DoubleQuote));
            }
        }
        if (customParameters is not null)
        {
            foreach (var (key, value) in customParameters)
            {
                replacements.Add(key, value is null ? Consts.Null : string.Concat(Consts.DoubleQuote, value, Consts.DoubleQuote));
            }
        }
        if (tokenSet is not null && (antiforgeryFieldNameTag is not null || antiforgeryTokenTag is not null))
        {
            if (antiforgeryFieldNameTag is not null)
            {
                replacements.Add(antiforgeryFieldNameTag, tokenSet.FormFieldName);
            }
            if (antiforgeryTokenTag is not null && tokenSet.RequestToken is not null)
            {
                replacements.Add(antiforgeryTokenTag, tokenSet.RequestToken);
            }
        }
        return Formatter.FormatString(input, replacements);
    }
}