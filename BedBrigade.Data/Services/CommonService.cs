﻿using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class CommonService : ICommonService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;

        public CommonService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService)
        {
            _cachingService = cachingService;
            _contextFactory = contextFactory;
        }
        
        public async Task<ServiceResponse<List<TEntity>>> GetAllForLocationAsync<TEntity>(IRepository<TEntity> repository, int locationId) where TEntity : class, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetAllForLocationAsync with LocationId {locationId}");
            var cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache with LocationId {locationId}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {repository.GetEntityName()} records with LocationId {locationId}", true, result);
            }
        }


        public async Task<ServiceResponse<List<string>>> GetDistinctEmail<TEntity>(IRepository<TEntity> repository)
            where TEntity : class, IEmail
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetDistinctEmail");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Select(b => b.Email).Distinct().ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
            }
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation<TEntity>(IRepository<TEntity> repository, int locationId) where TEntity : class, IEmail, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetDistinctEmailByLocation({locationId}");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(o => o.LocationId == locationId).Select(b => b.Email).Distinct().ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
            }
        }

        public async Task<ServiceResponse<List<TEntity>>> GetAllForLocationList<TEntity>(IRepository<TEntity> repository, List<int> locationIds) where TEntity : class, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetAllForLocationList with LocationIds {string.Join(",", locationIds)}");
            var cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache with LocationIds {string.Join(",", locationIds)}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => locationIds.Contains(b.LocationId)).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {repository.GetEntityName()} records with LocationIds {string.Join(",", locationIds)}", true, result);
            }
        }
    }


}
