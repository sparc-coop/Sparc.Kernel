﻿using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Data;
using System.Linq.Expressions;

namespace Sparc.Blossom.Data;

public class SqlServerRepository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    protected readonly DbContext context;
    protected DbSet<T> Command => context.Set<T>();
    public IQueryable<T> Query => context.Set<T>().AsNoTracking();

    public SqlServerRepository(DbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<T?> FindAsync(object id)
    {
        return await Command.FindAsync(id);
    }

    public async Task<T?> FindAsync(Expression<Func<T, bool>> expression)
    {
        return await Command.Where(expression).FirstOrDefaultAsync();
    }

    public async Task<T?> FindAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> expression)
    {
        return await Command.Where(expression).CountAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await CountAsync(spec, default);
    }

    public async Task<bool> AnyAsync(ISpecification<T> spec)
    {
        return await AnyAsync(spec, default);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await Command.ToListAsync();
    }

    public async Task<List<T>> GetAllAsync(ISpecification<T> spec)
    {
        return await ListAsync(spec);
    }

    public async Task AddAsync(T item)
    {
        Command.Add(item);
        await CommitAsync();
    }

    public async Task UpdateAsync(T item)
    {
        Command.Update(item);
        await CommitAsync();
    }

    public async Task DeleteAsync(T item)
    {
        Command.Remove(item);
        await CommitAsync();
    }

    public async Task ExecuteAsync(object id, Action<T> action)
    {
        var entity = await context.Set<T>().FindAsync(id);
        if (entity == null)
            throw new Exception($"Item with id {id} not found");

        action(entity);
        await CommitAsync();
    }

    public async Task ExecuteAsync(T entity, Action<T> action)
    {
        action(entity);
        await CommitAsync();
    }

    public IQueryable<T> Include(params string[] path)
    {
        return Include(Command, path);
    }

    private IQueryable<T> Include(IQueryable<T> source, params string[] path)
    {
        foreach (var item in path)
        {
            source = source.Include(item);
        }

        return source;
    }

    private async Task CommitAsync()
    {
        await context.SaveChangesAsync();
    }

    public IQueryable<T> FromSqlRaw(string sql, params object[] parameters)
    {
        return context.Set<T>().FromSqlRaw(sql, parameters);
    }

    public Task<List<U>> FromSqlAsync<U>(string sql, params (string, object)[] parameters)
    {
        var isStoredProcedure = sql.StartsWith("EXEC ");
        var commandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

        if (isStoredProcedure)
            sql = sql.Replace("EXEC ", "");

        var p = new DynamicParameters();
        if (parameters != null)
            foreach (var parameter in parameters)
            {
                var key = (parameter.Item1.Contains("@") ? "" : "@") + parameter.Item1;
                p.Add(key, parameter.Item2);
            }

        var result = context.Database.GetDbConnection().Query<U>(sql, p, commandType: commandType).ToList();

        return Task.FromResult(result);
    }
   
    public async Task AddAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Command.Add(item);

        await CommitAsync();
    }

    public async Task UpdateAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Command.Update(item);

        await CommitAsync();
    }

    public async Task DeleteAsync(IEnumerable<T> items)
    {
        foreach (var item in items)
            Command.Remove(item);

        await CommitAsync();
    }

    public async Task UpdateWhereAsync(Expression<Func<T, bool>> where, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> update)
    {
        await Command.Where(where).ExecuteUpdateAsync(update);
    }
}
