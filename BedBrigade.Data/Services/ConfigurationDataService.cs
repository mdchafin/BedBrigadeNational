﻿using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using BedBrigade.Common;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Services;

public class ConfigurationDataService : IConfigurationDataService
{
    private const string CacheSection = "Configuration";
    private const string FoundRecord = "Found Record";
    private readonly DataContext _context;
    private readonly ICachingService _cachingService;

    public ConfigurationDataService(DataContext context, ICachingService cachingService)
    {
        _context = context;
        _cachingService = cachingService;
    }

    public async Task<ServiceResponse<Configuration>> GetAsync(string configurationkey)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, configurationkey);
        var cachedContent = _cachingService.Get<Configuration>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<Configuration>(FoundRecord, true, cachedContent); ;

        var result = await _context.Configurations.FindAsync(configurationkey);
        if (result != null)
        {
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<Configuration>(FoundRecord, true, result);
        }
        return new ServiceResponse<Configuration>("Not Found");
    }

    public async Task<ServiceResponse<List<Configuration>>> GetAllAsync()
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, "GetAllAsync");
        var cachedContent = _cachingService.Get<List<Configuration>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<Configuration>>($"Found {cachedContent.Count} records.", true, cachedContent); ;

        var result = await _context.Configurations.ToListAsync();
        if (result != null)
        {
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Configuration>>($"Found {result.Count} records.", true, result);
        }
        return new ServiceResponse<List<Configuration>>("None found.");
    }

    /// <summary>
    /// Get all the configuration record within a given section, e.g. System, Email, Media
    /// </summary>
    /// <param name="section"></param>
    /// <returns>List of configuration records from a given section </returns>
    public async Task<ServiceResponse<List<Configuration>>> GetAllAsync(ConfigSection section)
    {
        string cacheKey = _cachingService.BuildCacheKey(CacheSection, $"GetAllAsync{section}");
        var cachedContent = _cachingService.Get<List<Configuration>>(cacheKey);

        if (cachedContent != null)
            return new ServiceResponse<List<Configuration>>($"Found {cachedContent.Count} records.", true, cachedContent); ;

        var result = await _context.Configurations.Where(c => c.Section == section).ToListAsync();
        if (result != null)
        {
            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Configuration>>(FoundRecord, true, result);
        }
        return new ServiceResponse<List<Configuration>>("Not Found");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(string configurationKey)
    {
        var config = await _context.Configurations.FindAsync(configurationKey);
        if (config == null)
        {
            return new ServiceResponse<bool>($"Configuration record with key {configurationKey} not found");
        }
        try
        {
            _context.Configurations.Remove(config);
            await _context.SaveChangesAsync();
            _cachingService.ClearAll();
            return new ServiceResponse<bool>($"Removed record with key {configurationKey}.", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<bool>($"DB error on delete of configuration record with key {configurationKey} - {ex.Message} ({ex.ErrorCode})");
        }
    }

    public async Task<ServiceResponse<Configuration>> UpdateAsync(Configuration configuration)
    {
        var config = await _context.Configurations.FindAsync(configuration.ConfigurationKey);
        config.ConfigurationValue = configuration.ConfigurationValue;
        var result = await Task.Run(() => _context.Configurations.Update(config));
        if (result != null)
        {
            try
            { 
                await _context.SaveChangesAsync();
                
                //Enable or disable caching if it has changed
                if (config.ConfigurationKey == ConfigNames.IsCachingEnabled)
                {
                    bool isCachingEnabled = false;
                    if (Boolean.TryParse(config.ConfigurationValue, out isCachingEnabled))
                    {
                        _cachingService.IsCachingEnabled = isCachingEnabled;
                    }
                }

                _cachingService.ClearAll();
            }
            catch(DbException ex)
            {
                Log.Logger.Error("Database exception {0}", ex.ToString());                
            }
            catch(Exception ex)
            {
                Log.Logger.Error("Error saving configuration {0} ", ex.Message);
            }
            return new ServiceResponse<Configuration>($"Updated location with key {configuration}", true);
        }
        return new ServiceResponse<Configuration>($"User with key {configuration} was not updated.");
    }

    public async Task<ServiceResponse<Configuration>> CreateAsync(Configuration configuration)
    {
        try
        {
            await _context.Configurations.AddAsync(configuration);
            await _context.SaveChangesAsync();
            _cachingService.ClearAll();
            return new ServiceResponse<Configuration>($"Added configuration with key {configuration.ConfigurationKey}", true);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<Configuration>($"DB error on delete of user record with key {configuration.ConfigurationKey} - {ex.Message} ({ex.ErrorCode})");
        }

    }

}



