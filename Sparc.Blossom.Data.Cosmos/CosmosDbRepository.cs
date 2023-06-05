﻿using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbRepository<T> : RepositoryBase<T>, IRepository<T> where T : class, IRoot<string>
{
    public IQueryable<T> Query { get; }
    public DbContext Context { get; }
    protected CosmosDbDatabaseProvider DbProvider { get; }

    private static bool IsCreated;

    public CosmosDbRepository(DbContext context, CosmosDbDatabaseProvider dbProvider) : base(context)
    {
        Context = context;
        DbProvider = dbProvider;
        if (!IsCreated)
        {
            Context.Database.EnsureCreatedAsync().Wait();
            IsCreated = true;
        }
        //Mediator = mediator;
        Query = context.Set<T>();
    }

    public async Task<T?> FindAsync(object id)
    {
        if (id is string sid)
            return Context.Set<T>().FirstOrDefault(x => x.Id == sid);

        return await Context.Set<T>().FindAsync(id);
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await CountAsync(spec, default);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return await AnyAsync(spec, default);
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task AddAsync(T item)
    {
        await AddAsync(new[] { item });
    }

    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Context.Add(item);

        await SaveChangesAsync();
    }

    public async Task UpdateAsync(T item)
    {
        await UpdateAsync(new[] { item });
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            var existing = await FindAsync(item.Id);
            if (existing != null)
            {
                Context.Entry(existing).State = EntityState.Detached;
                Context.Add(item);
                Context.Update(item);
            }
            else
            {
                Context.Add(item);
            }
        }

        await Context.SaveChangesAsync();
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await FindAsync(id);
        if (entity == null)
            throw new Exception($"Item with id {id} not found");

        await ExecuteAsync(entity, action);
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await UpdateAsync(entity);
    }

    public async Task DeleteAsync(T item)
    {
        await DeleteAsync(new[] { item });
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Context.Set<T>().Remove(item);
        
        await Context.SaveChangesAsync();
    }

    private async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync().ConfigureAwait(false);
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        return CosmosQueryableExtensions.FromSqlRaw(Context.Set<T>(), sql, parameters);
    }

    public IQueryable<T> PartitionQuery(string partitionKey)
    {
        return Query.WithPartitionKey(partitionKey);
    }

    public async Task<List<TOut>> FromSqlAsync<TOut>(string sql, params object[] parameters)
    {
        return await FromSqlAsync<TOut>(sql, null, parameters);
    }

    public async Task<List<TOut>> FromSqlAsync<TOut>(string sql, string? partitionKey, params object[] parameters)
    {
        var container = DbProvider.Database.GetContainer(Context.GetType().Name);
        var requestOptions = partitionKey == null
            ? null
            : new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };

        sql = sql.Replace("{", "@p").Replace("}", "");
        var query = new QueryDefinition(sql);
        if (parameters != null)
        {
            var p = 0;
            foreach (var parameter in parameters)
            {
                var key = $"@p{p++}";
                query = query.WithParameter(key, parameter);
            }
        }
        var results = container.GetItemQueryIterator<TOut>(query,
            requestOptions: requestOptions);

        var list = new List<TOut>();

        while (results.HasMoreResults)
            list.AddRange(await results.ReadNextAsync());

        return list;
    }
}
