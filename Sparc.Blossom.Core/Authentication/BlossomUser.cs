﻿using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomUser(string username, string authenticationType, string externalId) : BlossomEntity<string>
{
    public string Username { get; set; } = username;
    public string AuthenticationType { get; set; } = authenticationType;
    public string ExternalId { get; set; } = externalId;

    public Dictionary<string, string> Claims { get; set; } = [];
    public Dictionary<string, IEnumerable<string>> MultiClaims { get; set; } = [];

    protected void AddClaim(string type, string? value)
    {
        if (value == null)
            return;
        
        if (Claims.ContainsKey(type))
            Claims[type] = value;
        else
            Claims.Add(type, value);
    }

    protected void AddClaim(string type, IEnumerable<string> values)
    {
        if (values == null || !values.Any())
            return;

        if (!MultiClaims.ContainsKey(type))
            MultiClaims.Add(type, values);
        else
            MultiClaims[type] = values;
    }

    protected virtual void RegisterClaims()
    {
        // Do nothing in base class. This should be overridden in derived classes to
        // create the claims from the persisted user.
    }

    public virtual ClaimsPrincipal CreatePrincipal()
    {
        AddClaim(ClaimTypes.NameIdentifier, Id);
        AddClaim(ClaimTypes.Name, Username);
        RegisterClaims();

        var claims = Claims.Select(x => new Claim(x.Key, x.Value)).ToList();
        claims.AddRange(MultiClaims.SelectMany(x => x.Value.Select(v => new Claim(x.Key, v))));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Blossom"));
    }

    public void ChangeUsername(string username)
    {
        Username = username;
    }

    public void ChangeAuthenticationType(string authenticationType, string externalId)
    {
        AuthenticationType = authenticationType;
        ExternalId = externalId;
    }
}
