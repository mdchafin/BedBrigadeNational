﻿using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ICommonService
    {
        Task<ServiceResponse<List<TEntity>>> GetAllForLocationAsync<TEntity>(IRepository<TEntity> repository, int locationId)
            where TEntity : class, ILocationId;

        Task<ServiceResponse<List<string>>> GetDistinctEmail<TEntity>(IRepository<TEntity> repository)
            where TEntity : class, IEmail;

        Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation<TEntity>(IRepository<TEntity> repository, int locationId)
            where TEntity : class, IEmail, ILocationId;

        Task<ServiceResponse<List<TEntity>>> GetAllForLocationList<TEntity>(IRepository<TEntity> repository, List<int> locationIds)
            where TEntity : class, ILocationId;
    }
}
