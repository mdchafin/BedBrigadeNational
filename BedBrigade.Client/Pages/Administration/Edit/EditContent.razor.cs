﻿using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.RichTextEditor;
using System.Security.Claims;
using BedBrigade.Client.Components;

namespace BedBrigade.Client.Pages.Administration.Edit
{
    public partial class EditContent : ComponentBase
    {
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private IContentService _svcContent { get; set; }
        [Inject] private NavigationManager _nm { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Parameter] public string Location { get; set; }
        [Parameter] public string ContentName { get; set; }
        private SfRichTextEditor RteObj { get; set; }
        private string? WorkTitle { get; set; }
        private string? Body { get; set; }
        private Content? Content { get; set; }
        private ClaimsPrincipal? Identity { get; set; }

        private List<ToolbarItemModel> Tools = new List<ToolbarItemModel>()
        {
             new ToolbarItemModel() {Command = ToolbarCommand.Bold },
             new ToolbarItemModel() {Command = ToolbarCommand.Italic},
             new ToolbarItemModel() {Command = ToolbarCommand.Underline },
             new ToolbarItemModel() {Command = ToolbarCommand.Alignments },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.OrderedList },
             new ToolbarItemModel() {Command = ToolbarCommand.UnorderedList },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Indent },
             new ToolbarItemModel() {Command = ToolbarCommand.Outdent },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.ClearFormat },
             new ToolbarItemModel() {Command = ToolbarCommand.RemoveLink },
             new ToolbarItemModel() {Command = ToolbarCommand.SourceCode },
             new ToolbarItemModel() {Command = ToolbarCommand.FullScreen },
             new ToolbarItemModel() {Command = ToolbarCommand.LowerCase },
             new ToolbarItemModel() {Command = ToolbarCommand.UpperCase },
             new ToolbarItemModel() {Command = ToolbarCommand.SuperScript },
             new ToolbarItemModel() {Command = ToolbarCommand.FontName },
             new ToolbarItemModel() {Command = ToolbarCommand.FontColor },
             new ToolbarItemModel() {Command = ToolbarCommand.FontSize },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.BackgroundColor },
             new ToolbarItemModel() {Command = ToolbarCommand.Formats },
             new ToolbarItemModel() {Command = ToolbarCommand.ClearFormat },
             new ToolbarItemModel() {Command = ToolbarCommand.FullScreen },
             new ToolbarItemModel() { Command = ToolbarCommand.Separator },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateLink },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateTable },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Redo },
             new ToolbarItemModel() {Command = ToolbarCommand.Undo }

        };


        protected override async Task OnInitializedAsync()
        {
            Identity = (await _authState.GetAuthenticationStateAsync()).User;
            WorkTitle = $"Editing {ContentName}";

            int locationId;

            if (!int.TryParse(Location, out locationId))
            {
                _toastService.Open(new ToastOptions
                {
                    Title = "Error",
                    Content = $"Could not parse location as integer: {Location}",
                    CssClass = "e-toast-danger",
                    Icon = "e-error toast-icons"
                });
            }

            var contentResult = await _svcContent.GetAsync(ContentName, locationId);

            if (contentResult.Success && contentResult.Data != null)
            {
                Body = contentResult.Data.ContentHtml;
                Content = contentResult.Data;
                Content.UpdateDate = DateTime.Now;
                Content.UpdateUser =  Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            }
            else
            {
                //https://www.syncfusion.com/forums/173239/how-to-keep-toast-visible-when-navigating-to-another-page
                _toastService.Open(new ToastOptions
                {
                    Title = "Error",
                    Content = $"Content not found for location {Location} with name of {ContentName}",
                    CssClass = "e-toast-danger",
                    Icon = "e-error toast-icons"
                });
            }
        }

        private async Task HandleSaveClick()
        {
            Content.ContentHtml = await RteObj.GetXhtmlAsync();

            //Update Content  Record
            var updateResult = await _svcContent.UpdateAsync(Content);
            if (updateResult.Success)
            {
                _toastService.Open(new ToastOptions
                {
                    Title = "Content Saved",
                    Content = $"Content saved for location {Location} with name of {ContentName}",
                    CssClass = "e-toast-success",
                    Icon = "e-success toast-icons"
                });
                _nm.NavigateTo("/administration/manage/pages");
            }
            else
            {
                _toastService.Open(new ToastOptions
                {
                    Title = "Error",
                    Content = $"Could not save Content for location {Location} with name of {ContentName}",
                    CssClass = "e-toast-danger",
                    Icon = "e-error toast-icons"
                });
            }
            
        }

        private async Task HandleCancelClick()
        {
            _nm.NavigateTo("/administration/manage/pages");
        }
    }
}
