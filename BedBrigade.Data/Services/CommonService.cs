﻿using BedBrigade.Common;
using BedBrigade.Data.Models;
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
        
        public async Task<ServiceResponse<List<TEntity>>> GetAllForLocationAsync<TEntity>(IRepository<TEntity> repository) where TEntity : class, ILocationId
        {
            string? roleName = await repository.GetUserRole();

            if (roleName == null)
                return new ServiceResponse<List<TEntity>>("No Role found");

            int locationId = await repository.GetUserLocationId();

            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetAllForLocationAsync with LocationId ({locationId})");
            var cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                if (roleName.ToLower() != RoleNames.NationalAdmin.ToLower())
                {
                    var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
                }

                var nationalAdminResponse = await dbSet.ToListAsync();
                _cachingService.Set(cacheKey, nationalAdminResponse);
                return new ServiceResponse<List<TEntity>>($"Found {nationalAdminResponse.Count()} {repository.GetEntityName()} records", true, nationalAdminResponse);
            }
        }
    }


}
