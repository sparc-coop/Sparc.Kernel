﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sparc.Blossom.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmos<T>(this IServiceCollection services, string connectionString, string databaseName, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : DbContext
    {
        // Bug fix for Sparc Realtime (events executing in parallel with a scoped context)
        services.AddDbContext<T>(options => options.UseCosmos(connectionString, databaseName, options =>
        {
            options.ConnectionMode(ConnectionMode.Direct);
        }), serviceLifetime);
        services.Add(new ServiceDescriptor(typeof(DbContext), typeof(T), serviceLifetime));
        services.AddTransient(sp => new CosmosDbDatabaseProvider(sp.GetRequiredService<DbContext>(), databaseName));

        services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(CosmosDbRepository<>), serviceLifetime));

        return services;
    }

    public static IServiceCollection AddCosmos<T>(this IServiceCollection services, IConfiguration configuration, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : DbContext
    {
        var connectionString = configuration.GetConnectionString("Database");
        var databaseName = configuration["Database"];
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
            throw new InvalidOperationException("Please provide a database connection string (in appsettings.json ConnectionStrings, named Database) and a database name (in appsettings.json, named Database).");
        
        // Bug fix for Blossom Realtime (events executing in parallel with a scoped context)
        services.AddDbContext<T>(options => options.UseCosmos(connectionString, databaseName, options =>
        {
            options.ConnectionMode(ConnectionMode.Direct);
        }), serviceLifetime);
        
        services.Add(new ServiceDescriptor(typeof(DbContext), typeof(T), serviceLifetime));
        services.AddTransient(sp => new CosmosDbDatabaseProvider(sp.GetRequiredService<DbContext>(), databaseName));

        services.Add(new ServiceDescriptor(typeof(IRepository<>), typeof(CosmosDbRepository<>), serviceLifetime));
        services.Add(new ServiceDescriptor(typeof(IRevisionRepository<>), typeof(CosmosDbRevisionRepository<>), serviceLifetime));

        return services;
    }

    public static ModelBuilder EnableRevisions(this ModelBuilder builder, string revisionContainerName)
    {
        var revisionableEntities = builder.Model.GetEntityTypes().Where(x => typeof(IHasRevision).IsAssignableFrom(x.ClrType));

        foreach (var entityType in revisionableEntities)
            builder.Entity(typeof(BlossomRevision<>).MakeGenericType(entityType.ClrType)).ToContainer(revisionContainerName);

        return builder;
    }
}
