﻿using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom;

public static class RepositoryExtensions
{
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable)
    {
        return EntityFrameworkQueryableExtensions.ToListAsync(queryable);
    }
}