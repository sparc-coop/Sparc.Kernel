﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sparc.Blossom.Data;
using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Sparc.Blossom.Server;
using Sparc.Blossom.Api;
using Sparc.Blossom.Authentication;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Sparc.Blossom;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddBlossom<TUser>(this WebApplicationBuilder builder, Action<IServiceCollection, IConfiguration>? services = null, IComponentRenderMode? renderMode = null) 
        where TUser : BlossomUser, new() 
    {
        var razor = builder.Services.AddRazorComponents();
        renderMode ??= RenderMode.InteractiveAuto;

        if (renderMode == RenderMode.InteractiveServer || renderMode == RenderMode.InteractiveAuto)
            razor.AddInteractiveServerComponents();
        if (renderMode == RenderMode.InteractiveWebAssembly || renderMode == RenderMode.InteractiveAuto)
            razor.AddInteractiveWebAssemblyComponents();

        builder.AddBlossomAuthentication<TUser>();

        services?.Invoke(builder.Services, builder.Configuration);

        builder.Services.AddBlossomContexts(Assembly.GetCallingAssembly());
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<AdditionalAssembliesProvider>();

        builder.AddBlossomRepository();

        return builder;
    }

    public static WebApplication UseBlossom<T>(this WebApplicationBuilder builder)
    {
        builder.Services.AddServerSideBlazor();
        builder.Services.AddOutputCache();

        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1"));

            if (builder.IsWebAssembly())
                app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        var razor = app.MapRazorComponents<T>();

        if (builder.IsServer())
            razor.AddInteractiveServerRenderMode();

        if (builder.IsWebAssembly())
            razor.AddInteractiveWebAssemblyRenderMode();

        if (Assembly.GetCallingAssembly() != typeof(T).Assembly)
            razor.AddAdditionalAssemblies(typeof(T).Assembly);

        app.MapBlossomContexts(Assembly.GetCallingAssembly());

        return app;
    }

    public static bool IsWebAssembly(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ImplementationType?.Name.Contains("WebAssemblyEndpointProvider") == true);

    public static bool IsServer(this WebApplicationBuilder builder) => builder.Services.Any(x => x.ImplementationType?.Name.Contains("CircuitEndpointProvider") == true);

    public static IApplicationBuilder UseCultures(this IApplicationBuilder app, string[] supportedCultures)
    {
        app.UseRequestLocalization(options => options
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures));

        return app;
    }
    public static IApplicationBuilder UseAllCultures(this IApplicationBuilder app)
    {
        var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .Select(x => x.Name)
            .ToArray();
        
        app.UseCultures(allCultures);

        return app;
    }
   
}
