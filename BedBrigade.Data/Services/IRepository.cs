﻿using BedBrigade.Data.Models;

namespace BedBrigade.Data.Services
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<ServiceResponse<List<TEntity>>> GetAllAsync();
        Task<ServiceResponse<TEntity>> GetByIdAsync(object id);
        Task<ServiceResponse<TEntity>> CreateAsync(TEntity entity);
        Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity);
        Task<ServiceResponse<bool>> DeleteAsync(object id);
    }
}