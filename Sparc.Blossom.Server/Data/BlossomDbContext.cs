﻿using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public class BlossomContext(DbContextOptions options, BlossomNotifier notifier) : DbContext(options)
{
    public BlossomNotifier Notifier { get; } = notifier;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new BlossomPropertyDiscoveryConvention());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEventsAsync();
        return result;
    }

    public void SetPublishStrategy(PublishStrategy strategy)
    {
        Notifier.SetPublishStrategy(strategy);
    }

    async Task DispatchDomainEventsAsync()
    {
        var domainEvents = ChangeTracker.Entries<Entity>().SelectMany(x => x.Entity.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Notifier.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}
