﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using KellermanSoftware.AddressParser;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using System.Diagnostics;
using BedBrigade.Client.Services;

namespace BedBrigade.Data.Services;

public class LocationDataService : Repository<Location>, ILocationDataService
{
    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly IAuthService _authService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IContentDataService _contentDataService;

    public LocationDataService(IDbContextFactory<DataContext> contextFactory,
        ICachingService cachingService,
        IAuthService authService,
        IConfigurationDataService configurationDataService,
        IContentDataService contentDataService) : base(contextFactory, cachingService, authService)
    {
        _cachingService = cachingService;
        _configurationDataService = configurationDataService;
        _contentDataService = contentDataService;
        _contextFactory = contextFactory;
        _authService = authService;
    }

    public override async Task<ServiceResponse<Location>> CreateAsync(Location entity)
    {
        try
        {
            var groveCityLocation = await GetByIdAsync((int) LocationNumber.GroveCity);

            if (!groveCityLocation.Success || groveCityLocation.Data == null)
            {
                return new ServiceResponse<Location>("Grove City location not found", false);
            }

            var existingLocation = await GetLocationByRouteAsync(entity.Route);

            if (existingLocation.Success)
            {
                return new ServiceResponse<Location>($"Location with route {entity.Route} already exists");
            }

            var result = await base.CreateAsync(entity);

            if (!result.Success)
                return result;

            var location = result.Data;

            FileUtil.CreateLocationMediaDirectory(location);
            await CreateContent(location, ContentType.Header, "LocationHeader.html", "Header");
            await CreateContent(location, ContentType.Footer, "LocationFooter.html", "Footer");
            await CreateContent(location, ContentType.Body, "LocationHome.html", "Home"); 
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Home");
            await CreateContent(location, ContentType.Body, "LocationDonations.html", "Donations");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Donations");
            await CreateContent(location, ContentType.Body, "LocationAboutUs.html", "AboutUs");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "AboutUs");
            await CreateContent(location, ContentType.Body, "LocationAssemblyInstructions.html",  "Assembly-Instructions");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Assembly-Instructions");
            // add Delivery Check List - new 9/6/2024
            await CreateContent(location, ContentType.DeliveryCheckList, "DeliveryCheckList.txt", "DeliveryCheckList");
            await CreateContent(location, ContentType.EmailTaxForm, "EmailTaxForm.txt", "EmailTaxForm");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public async Task CreateContent(Location location, ContentType contentType, string templateName, string name)
    {
        var content = new Content();
        content.LocationId = location.LocationId;
        content.ContentType = contentType;
        content.Name = name;
        content.ContentHtml = WebHelper.GetHtml(templateName);
        content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
        content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);
        content.ContentHtml = content.ContentHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");
        content.Title = StringUtil.InsertSpaces(content.Name);
        await _contentDataService.CreateAsync(content);
    }




    public async Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(),  $"GetLocationByRouteAsync for ({routeName})");
        var cachedLocation = _cachingService.Get<Location>(cacheKey);

        if (cachedLocation != null)
            return new ServiceResponse<Location>($"{routeName} found in cache", true, cachedLocation);

        using (var ctx = _contextFactory.CreateDbContext())
        {
            var loc = await ctx.Locations.FirstOrDefaultAsync(l => l.Route == routeName);
            if (loc != null)
            {
                _cachingService.Set(cacheKey, loc);
                return new ServiceResponse<Location>($"{routeName} found", true, loc);
            }
            return new ServiceResponse<Location>("Not Found");
        }
    }

    public async Task<List<Location>> GetAvailableLocationsAsync(List<Location> locations)
    {
        bool isAuthenticated = _authService.IsLoggedIn;

       // Debug.WriteLine("User authenticated -  " + isAuthenticated.ToString());

        // Step 1: Filter locations by postal code
        var filteredLocations = locations.Where(l => !string.IsNullOrEmpty(l.PostalCode)).ToList();

        // Step 2: If user is not authenticated, select only active locations
        if (!isAuthenticated)
        {
            filteredLocations = filteredLocations.Where(l => l.IsActive).ToList();
        }
        else
        {
            // Step 3: If user is authenticated, mark inactive locations with an asterisk
            filteredLocations.ForEach(l =>
            {
                if (!l.IsActive)
                {
                    l.Name = $"*{l.Name}";
                }
            });
        }

        return filteredLocations;
    } // get available location

    public async Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string postalCode)
    {
        AddressParser parser = LibraryFactory.CreateAddressParser();

        if (!parser.IsValidZipCode(postalCode))
        {
            return new ServiceResponse<List<LocationDistance>>($"Invalid postal code {postalCode}");
        }

        var zipCodeInfo = parser.GetInfoForZipCode(postalCode);
        
        if (zipCodeInfo.Latitude == null || zipCodeInfo.Longitude == null)
        {
            return new ServiceResponse<List<LocationDistance>>($"Postal code {postalCode} is a military or PO Box and cannot be used");
        }

        int maxMiles;
        var milesResult = await _configurationDataService.GetByIdAsync(ConfigNames.BedBrigadeNearMeMaxMiles);
        if (milesResult.Success && milesResult.Data != null && milesResult.Data.ConfigurationValue != null)
        {
            maxMiles = int.Parse(milesResult.Data.ConfigurationValue);
        }
        else
        {
            return new ServiceResponse<List<LocationDistance>>($"BedBrigadeNearMeMaxMiles is not set or is not an integer");
        }

        var locationsResult = await GetAllAsync();
        bool anyPostalCodes = locationsResult.Data.Any(l => !string.IsNullOrEmpty(l.PostalCode));

        if (!anyPostalCodes)
        {
            return new ServiceResponse<List<LocationDistance>>($"No locations have postal codes");
        }


        var AvailableLocations = await GetAvailableLocationsAsync(locationsResult.Data.ToList());

        var result = new List<LocationDistance>();
        foreach (var loc in AvailableLocations) // locationsResult.Data
        {
            var distance = parser.GetDistanceInMilesBetweenTwoZipCodes(postalCode, loc.PostalCode);
                    if (distance < maxMiles)
                    {
                        LocationDistance locationDistance = new LocationDistance();
                        locationDistance.LocationId = loc.LocationId;
                        locationDistance.Name = loc.Name;
                        locationDistance.Route = loc.Route;
                        locationDistance.Distance = distance;
                        result.Add(locationDistance);
                    }                           
        }
        result = result.OrderBy(r => r.Distance).ToList();

        if (result.Count == 0)
        {
            return new ServiceResponse<List<LocationDistance>>($"No locations found within {maxMiles} miles of {postalCode}", true, result);
        }

        return new ServiceResponse<List<LocationDistance>>($"Found {result.Count} locations within {maxMiles} miles of {postalCode}", true, result);
    }

    public async Task<ServiceResponse<List<Location>>> GetLocationsByMetroAreaId(int metroAreaId)
    {
        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetLocationsByMetroAreaId for ({metroAreaId})");
        var cachedLocations = _cachingService.Get<List<Location>>(cacheKey);

        if (cachedLocations != null)
        {
            return new ServiceResponse<List<Location>>($"Found {cachedLocations.Count} locations for Metro Area ID {metroAreaId} in cache", true, cachedLocations);
        }

        try
        {
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var locations = await ctx.Locations
                    .Where(l => l.MetroAreaId == metroAreaId)
                    .ToListAsync();

                _cachingService.Set(cacheKey, locations);
                return new ServiceResponse<List<Location>>($"Found {locations.Count} locations for Metro Area ID {metroAreaId}", true, locations);
            }
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Location>>($"Error retrieving locations for Metro Area ID {metroAreaId}: {ex.Message} ({ex.ErrorCode})", false);
        }
    }


}





