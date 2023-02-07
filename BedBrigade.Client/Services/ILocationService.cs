﻿using BedBrigade.Data.Models;

namespace BedBrigade.Client.Services
{
    public interface ILocationService
    {
        Task<ServiceResponse<Location>> CreateAsync(Location location);
        Task<ServiceResponse<bool>> DeleteAsync(int locationId);
        Task<ServiceResponse<List<Location>>> GetAllAsync();
        Task<ServiceResponse<Location>> GetAsync(int locationId);
        Task<ServiceResponse<Location>> UpdateAsync(Location location);
    }
}