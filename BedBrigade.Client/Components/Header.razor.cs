using System.Security.Claims;
using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

using Microsoft.JSInterop;
using Serilog;

namespace BedBrigade.Client.Components
{
    public partial class Header : ComponentBase, IDisposable
    {
        // Client
        [Inject] private IJSRuntime _js { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private NavigationManager _nm { get; set; }
        [Inject] private ILocationState _locationState { get; set; }

        const string LoginElement = "loginElement";
        const string AdminElement = "adminElement";
        const string SetInnerHTML = "SetGetValue.SetInnerHtml";

        private string headerContent = string.Empty;
        protected string Login = "login";
        private bool IsAuthenicated { get; set; } = false;
        private string Menu { get; set; }

        private string PreviousLocation { get; set; } 
        private ClaimsPrincipal? User { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Log.Debug("Header.OnInitializedAsync");
            await LoadContent();
            _svcAuth.AuthChanged += OnAuthChanged;
            _locationState.OnChange += OnLocationChanged;
        }

        private Task OnAuthChanged(ClaimsPrincipal arg)
        {
            //We don't need to refresh if this is the first time to load the component
            if (User == null)
            {
                User = arg;
            }
            else
            {
                StateHasChanged();
            }

            return Task.CompletedTask;
        }


        private async Task OnLocationChanged()
        {
            await LoadContent();
            StateHasChanged();
        }

        private async Task LoadContent()
        {
            string locationName = _locationState.Location ?? SeedConstants.SeedNationalName;
            var locationResult = await _svcLocation.GetLocationByRouteAsync($"/{locationName.ToLower()}");

            if (!locationResult.Success)
            {
                Log.Logger.Error($"Error loading location: {locationResult.Message}");
            }
            else
            {
                var contentResult = await _svcContent.GetAsync("Header", locationResult.Data.LocationId);

                if (contentResult.Success)
                {
                    headerContent = contentResult.Data.ContentHtml;
                    PreviousLocation = locationName;
                }
                else
                {
                    Log.Logger.Error($"Error loading Header for LocationId {locationResult.Data.LocationId}: {contentResult.Message}");
                }
            }
        }

        public void Dispose()
        {
            _svcAuth.AuthChanged -= OnAuthChanged;
            _locationState.OnChange -= OnLocationChanged; // Unsubscribe from the event
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("Header.OnAfterRenderAsync");
            await HandleRender();
        }

        private async Task HandleRender()
        {
            Log.Debug("Header.HandleRender");

            try
            {
                if (_svcAuth.IsLoggedIn)
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Logout");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/logout");
                    await _js.InvokeVoidAsync("DisplayToggle.Show", "administration");
                    await ShowMenuItems();
                }
                else
                {
                    await _js.InvokeVoidAsync(SetInnerHTML, LoginElement, "Login");
                    await _js.InvokeVoidAsync("SetGetValue.SetAttribute", LoginElement, "href", "/login");
                    await _js.InvokeVoidAsync("DisplayToggle.HideByClass", "nadmin");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Header.HandleOtherRenders: {ex.Message}");
                throw;
            }
        }

        private async Task ShowMenuItems()
        {
            Log.Debug("Header.SetMenuItems");
            try
            {
                if (_svcAuth.CurrentUser.HasRole(RoleNames.NationalAdmin))
                {
                    await Show("nadmin");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.NationalEditor))
                {
                    await Show( "neditor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationAdmin))
                {
                    await Show( "ladmin");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationEditor))
                {
                    await Show( "leditor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationAuthor))
                {
                    await Show( "lauthor");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationScheduler))
                {
                    await Show( "lscheduler");
                }
                else if (_svcAuth.CurrentUser.HasRole(RoleNames.LocationTreasurer))
                {
                    await Show( "ltreasurer");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Error loading Menu: {ex.Message}");
            }
        
        }

        private async Task Show(string cssClass)
        {
            await _js.InvokeVoidAsync("DisplayToggle.ShowByClass", cssClass);
        }


    }
}