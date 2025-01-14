﻿using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticator<T>(IRepository<T> users) : IBlossomAuthenticator<T>, IBlossomAuthenticator
    where T : BlossomUser, new()
{
    public LoginStates LoginState { get; set; } = LoginStates.NotInitialized;

    public BlossomUser? GenericUser => User;
    public T? User { get; set; }
    public IRepository<T> Users { get; } = users;
    public string? Message { get; set; }

    public async Task<BlossomUser> GetGenericAsync(ClaimsPrincipal principal) => await GetAsync(principal);

    public async Task<T> GetAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            User = await Users.FindAsync(principal.Id());
        }

        if (User == null)
        {
            User = BlossomUser.FromPrincipal<T>(principal);
            await Users.AddAsync(User);
        }

        return User!;
    }

    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        principal = user.Login();
        await Users.UpdateAsync((T)user);
        return principal;
    }

    public virtual async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        var user = await GetAsync(principal);
        principal = user.Login(authenticationType, externalId);
        await Users.UpdateAsync((T)user);
        return principal;
    }

    public virtual async Task<ClaimsPrincipal> LogoutAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        principal = user.Logout();
        await Users.UpdateAsync((T)user);
        return principal;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public virtual async IAsyncEnumerable<LoginStates> Login(ClaimsPrincipal? principal, string? emailOrToken = null)
    {
        LoginState = LoginStates.LoggedIn;
        yield return LoginState;
    }

    public virtual async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal? principal)
    {
        LoginState = LoginStates.LoggedOut;
        yield return LoginState;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
