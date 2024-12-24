﻿namespace Sparc.Blossom;

public interface IRunner
{
    Task<BlossomAggregateMetadata> Metadata();
    Task Patch(object id, BlossomPatch changes);
    Task Execute(object id, string name, params object?[] parameters);
    Task Delete(object id);
    Task On(object id, string name, params object?[] parameters);
}

public interface IRunner<T> : IRunner
{
    Task<T> Create(params object?[] parameters);
    Task<T?> Get(object id);
    Task<IEnumerable<T>> ExecuteQuery(string? name = null, params object?[] parameters);
    Task<BlossomQueryResult<T>> ExecuteQuery(BlossomQueryOptions options);
    Task<T?> Undo(object id, long? revision);
    Task<T?> Redo(object id, long? revision);
}
