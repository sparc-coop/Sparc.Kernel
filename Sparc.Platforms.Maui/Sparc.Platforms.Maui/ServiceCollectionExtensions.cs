﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Sparc.Core;
using Sparc.Platforms.Maui.Platforms.Android;
using Sparc.Platforms.Maui.Platforms.iOS;

namespace Sparc.Platforms.Maui;

public static class ServiceCollectionExtensions
{
    public static MauiAppBuilder Sparcify<TMainLayout>(this MauiAppBuilder builder) where TMainLayout : LayoutComponentBase
    {
        builder.UseMauiApp<App>();
        
        builder.Services.AddBlazorWebView();
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<IErrorBoundaryLogger, ConsoleErrorBoundaryLogger>()
            .AddScoped<LayoutComponentBase, TMainLayout>()
            .AddSingleton<RootScope>();

        return builder;
    }

    public static MauiAppBuilder AddPushNotifications(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<PushTokenManager>();
#if ANDROID
        builder.Services.AddSingleton<IPushNotificationService, AndroidPushNotificationService>();
#elif IOS
        builder.Services.AddSingleton<IPushNotificationService, iOSPushNotificationService>();
#endif
        return builder;
    }


}
