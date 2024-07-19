﻿using Microsoft.EntityFrameworkCore;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public class BlossomDbContext(DbContextOptions options, IBlossomAuthenticator auth, BlossomNotifier notifier) : DbContext(options)
{
    public IBlossomAuthenticator Auth { get; } = auth;
    public BlossomNotifier Notifier { get; } = notifier;
    protected string UserId => Auth.User?.Id ?? "anonymous";

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
        var domainEvents = ChangeTracker.Entries<BlossomEntity>().SelectMany(x => x.Entity.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Notifier.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}
