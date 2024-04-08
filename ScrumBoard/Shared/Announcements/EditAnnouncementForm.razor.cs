
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Extensions;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Announcements;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;
using ScrumBoard.Utils;

namespace ScrumBoard.Shared.Announcements 
{
    public partial class EditAnnouncementForm : ComponentBase
    {
        [Inject]
        private IAnnouncementService AnnouncementService { get; set; }

        [CascadingParameter(Name="Self")]
        public User Self { get; set; }

        [Parameter]
        public Announcement Announcement { get; set; }
        
        [Parameter]
        public EventCallback CancelCallback { get; set; }
        
        [Parameter]
        public EventCallback AfterValidSubmitCallback { get; set; }

        private AnnouncementForm _announcementForm;

        private Announcement PreviewAnnouncement => _announcementForm?.ToAnnouncement();

        private bool _isNewAnnouncement = false;
        
        private const DurationFormatOptions DurationFormatOptions = Utils.DurationFormatOptions.UseDaysAsLargestUnit | Utils.DurationFormatOptions.FormatForLongString;
        
        // When showing or hiding the start/end date input, if we are hiding it, set the form's date
        // to null so that blazor doesn't try to validate anything, but also store the date so that
        // the user doesn't have to start from scratch if they open it again. If they are showing the
        // datepicker however, then pull out the stored date if there is one.
        private bool _hasStartDate = false;
        private bool HasStartDate
        {
            get => _hasStartDate;
            set
            {
                _hasStartDate = value;
                if(_hasStartDate && _startDateHolder.HasValue) _announcementForm.Start = _startDateHolder;
                if(!_hasStartDate) {
                    _startDateHolder = _announcementForm.Start;
                    _announcementForm.Start = null;
                }
            }
        }
        
        private DateTime? _startDateHolder;        

        private bool _hasEndDate = false;
        private bool HasEndDate
        {
            get => _hasEndDate;
            set
            {
                _hasEndDate = value;
                if(_hasEndDate && _endDateHolder.HasValue) _announcementForm.End = _endDateHolder;
                if(!_hasEndDate) {
                    _endDateHolder = _announcementForm.End;
                    _announcementForm.End = null;
                }
            }
        }
        private DateTime? _endDateHolder;

        private string _submitMessage;

        private string StartShowingPreviewText => 
            _hasStartDate && _announcementForm.Start.HasValue 
                ? $"{_announcementForm.Start.Value.ToLongDateString()} " + 
                    $"@ {_announcementForm.Start.Value.ToShortTimeString()} " +
                    $"(in {DurationUtils.DurationStringFrom(_announcementForm.Start.Value - DateTime.Now, DurationFormatOptions)})"
                : "Immediately";

        private string StopShowingPreviewText => 
            _hasEndDate && _announcementForm.End.HasValue 
                ? $"{_announcementForm.End.Value.ToLongDateString()} " + 
                    $"@ {_announcementForm.End.Value.ToShortTimeString()} " +
                    $"(in {DurationUtils.DurationStringFrom(_announcementForm.End.Value - DateTime.Now, DurationFormatOptions)})"
                : "When manually archived";

        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender) return;
            if (Announcement is null)
            {
                SetUpForNewAnnouncement();
            }
            else
            {
                SetUpForExistingAnnouncement(Announcement);
            }
        }

        private async Task HandleValidSubmit()
        {
            var newAnnouncementValue = _announcementForm.ToAnnouncement();

            if(_isNewAnnouncement) {
                newAnnouncementValue.CreatorId = Self.Id;
                newAnnouncementValue.LastEditorId = Self.Id;
                await AnnouncementService.AddNewAnnouncementAsync(newAnnouncementValue);
                SetUpForExistingAnnouncement(newAnnouncementValue);
                _submitMessage = "New announcement created successfully!";
            } else {
                newAnnouncementValue.Id = Announcement.Id;
                newAnnouncementValue.CreatorId = Announcement.CreatorId;
                newAnnouncementValue.Created = Announcement.Created;
                newAnnouncementValue.LastEditorId = Self.Id;
                await AnnouncementService.UpdateExistingAnnouncementAsync(newAnnouncementValue);
                SetUpForExistingAnnouncement(newAnnouncementValue);
                _submitMessage = "Changes saved successfully!";
            }

            await AfterValidSubmitCallback.InvokeAsync();
        }
        
        private void SetUpForExistingAnnouncement(Announcement announcementValue)
        {
            _isNewAnnouncement = false;
            Announcement = announcementValue;
            _announcementForm = Announcement.ToAnnouncementForm();
            _hasStartDate = Announcement.Start.HasValue;
            _hasEndDate = Announcement.End.HasValue;
            StateHasChanged();
        }

        private void SetUpForNewAnnouncement()
        {
            _isNewAnnouncement = true;
            Announcement = null;
            _announcementForm = new();
            StateHasChanged();
        }

        private async Task HandleCancel()
        {
            _submitMessage = "";
            if (CancelCallback.HasDelegate)
            {
                await CancelCallback.InvokeAsync();
            }
            else
            {
                SetUpForNewAnnouncement();
            }
        }
    }
}