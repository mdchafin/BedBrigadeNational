﻿using BedBrigade.Client.Components;
using BedBrigade.Client.Services;
using BedBrigade.Common.Logic;
using BedBrigade.Data;
using BedBrigade.Data.Seeding;
using BedBrigade.Data.Services;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;
using System.Diagnostics;
using BedBrigade.Common.Constants;

namespace BedBrigade.Client
{
    public static class StartupLogic
    {
        private static ServiceProvider _svcProvider;

        public static void LoadConfiguration(WebApplicationBuilder builder)
        {
            if (Debugger.IsAttached)
            {
                builder.Configuration.AddJsonFile("appsettings.Local.json", optional: false, reloadOnChange: true);
            }
            else if (Common.Logic.WebHelper.IsDevelopment())
            {
                builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            }
            else
            {
                builder.Configuration.AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true);
            }
        }

        public static void ConfigureLogger(WebApplicationBuilder builder)
        {
            //Read logging configuration from appsettings.json
            //See https://www.codeproject.com/Articles/5344667/Logging-with-Serilog-in-ASP-NET-Core-Web-API
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Logger = logger;
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);

            Log.Logger.Information("Starting Up");
        }

        public static void AddServicesToTheContainer(WebApplicationBuilder builder)
        {
            Log.Logger.Information("AddServicesToTheContainer");

            // Add services to the container.
            builder.Services.AddMvc(option => option.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseApplicationServiceProvider(_svcProvider);
            });

            builder.Services.AddHttpClient();

            SetupAuth(builder);
            ClientServices(builder);
            DataServices(builder);

            builder.Services.AddScoped<ToastService, ToastService>();
            builder.Services.AddSignalR(e =>
            {
                e.MaximumReceiveMessageSize = 1024000;
            });

            builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
            //services cors
            builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            // Syncfusion Blazor Licensing
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            builder.Services.AddSyncfusionBlazor();

            _svcProvider = builder.Services.BuildServiceProvider();
        }

        private static void SetupAuth(WebApplicationBuilder builder)
        {
            Log.Logger.Information("SetupAuth");
            builder.Services.AddBlazoredSessionStorage();
            builder.Services.AddScoped<ICustomSessionService, CustomSessionService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
        }

        private static void ClientServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("ClientServices");
            builder.Services.AddSingleton<ICachingService, CachingService>();
            builder.Services.AddScoped<ILoadImagesService, LoadImagesService>();
            builder.Services.AddScoped<ILocationState, LocationState>();
        }

        private static void DataServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("DataServices");
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<IDonationDataService, DonationDataService>();
            builder.Services.AddScoped<ILocationDataService, LocationDataService>();
            builder.Services.AddScoped<IVolunteerDataService, VolunteerDataService>();
            builder.Services.AddScoped<IConfigurationDataService, ConfigurationDataService>();
            builder.Services.AddScoped<IContentDataService, ContentDataService>();
            builder.Services.AddScoped<IVolunteerForDataService, VolunteerForDataService>();
            builder.Services.AddScoped<IMediaDataService, MediaDataService>();
            builder.Services.AddScoped<IDonationDataService, DonationDataService>();
            builder.Services.AddScoped<IBedRequestDataService, BedRequestDataService>();
            builder.Services.AddScoped<IScheduleDataService, ScheduleDataService>();
            builder.Services.AddScoped<ITemplateDataService, TemplateDataService>();
            builder.Services.AddScoped<IContactUsDataService, ContactUsDataService>();
            builder.Services.AddScoped<ISignUpDataService, SignUpDataService>();
            builder.Services.AddScoped<IEmailQueueDataService, EmailQueueDataService>();
            builder.Services.AddScoped<IMetroAreaDataService, MetroAreaDataService>();
            builder.Services.AddScoped<IHeaderMessageService, HeaderMessageService>();
            builder.Services.AddScoped<IUserPersistDataService, UserPersistDataService>();
            builder.Services.AddScoped<IDeliverySheetService, DeliverySheetService>();
            builder.Services.AddScoped<IMigrationDataService, MigrationDataService>();
        }

        public static WebApplication CreateAndConfigureApplication(WebApplicationBuilder builder)
        {
            Log.Logger.Information("Create and configure application");
            var app = builder.Build();

            app.UsePathBase("/National");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            return app;
        }

        public static async Task SetupDatabase(WebApplication app)
        {
            if (!ShouldSeed())
            {
                return;
            }

            Log.Logger.Information("Setup Database");

            //Create database if it does not exist
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
                using (var context = dbContextFactory.CreateDbContext())
                {
                    Log.Logger.Information("Performing Migration");
                    await context.Database.MigrateAsync();
                }
                Log.Logger.Information("Seeding Data");
                await Seed.SeedData(dbContextFactory);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "An error occurred during migration");
                Environment.Exit(1);
            }

        }

        public static bool ShouldSeed()
        {
            if (!Debugger.IsAttached)
            {
                Log.Logger.Information("Setup Database skipped because we are not in local development");
                return false;
            }
            string solutionDirectory = FileUtil.GetSolutionPath();
            string dataDirectory = Path.Combine(solutionDirectory, "BedBrigade.Data");

            if (!FileUtil.AnyCSharpFilesModifiedToday(dataDirectory))
            {
                Log.Logger.Information("Setup Database skipped because no .cs files have been modified today in " + dataDirectory);
                return false;
            }

            return true;
        }

        public static async Task SetupCaching(WebApplication app)
        {
            Log.Logger.Information("Setup Caching");
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                var config = await context.Configurations.FindAsync(ConfigNames.IsCachingEnabled);
                if (config != null)
                {
                    if (config != null)
                    {
                        var cachingService = services.GetRequiredService<ICachingService>();
                        cachingService.IsCachingEnabled = true;
                        bool isCachingEnabled;

                        if (bool.TryParse(config.ConfigurationValue, out isCachingEnabled))
                        {
                            cachingService.IsCachingEnabled = isCachingEnabled;
                        }

                        Log.Logger.Information(cachingService.IsCachingEnabled
                            ? "Caching is enabled"
                            : "Caching is disabled");
                    }
                }
            }
        }

        public static void SetupEmailQueueProcessing(WebApplication app)
        {
            Log.Logger.Information("Setup Email Queue Processing");
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var configurationDataService = services.GetRequiredService<IConfigurationDataService>();
            var emailQueueDataService = services.GetRequiredService<IEmailQueueDataService>();
            EmailQueueLogic.Start(emailQueueDataService, configurationDataService);
        }
    }
}
