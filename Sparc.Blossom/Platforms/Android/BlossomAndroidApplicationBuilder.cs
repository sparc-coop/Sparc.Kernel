﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.Android;

public class BlossomAndroidApplicationBuilder : IBlossomApplicationBuilder
{
    private readonly MauiAppBuilder MauiBuilder;

    public IServiceCollection Services => MauiBuilder.Services;

    public IConfiguration Configuration { get; }

    private bool _isAuthenticationAdded;

    public BlossomAndroidApplicationBuilder(string[] args)
    {
        MauiBuilder = MauiApp.CreateBuilder();

        MauiBuilder
            .UseMauiApp<MobileApp>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        MauiBuilder.Services.AddMauiBlazorWebView();

#if DEBUG
        MauiBuilder.Services.AddBlazorWebViewDeveloperTools();
        MauiBuilder.Logging.AddDebug();
#endif

        Configuration = new ConfigurationBuilder()
            .Build();

    }

    public void AddAuthentication<TUser>() where TUser : BlossomUser, new()
    {

        Services.AddScoped<BlossomDefaultAuthenticator<TUser>>()
                .AddScoped<IBlossomAuthenticator, BlossomDefaultAuthenticator<TUser>>();

        _isAuthenticationAdded = true;
    }

    public IBlossomApplication Build()
    {

        if (!_isAuthenticationAdded)
        {
            AddAuthentication<BlossomUser>();
        }


        var mauiApp = MauiBuilder.Build();

        return new BlossomAndroidApplication(mauiApp);
    }
}
