using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Repositories;
using ScrumBoard.Repositories.Changelog;

namespace ScrumBoard.Shared
{
    public partial class EditOverheadEntry : BaseProjectScopedForm
    {
        [CascadingParameter(Name = "Self")]
        public User Self { get; set; }
        
        [Inject]
        private IOverheadSessionRepository OverheadSessionRepository { get; set; }
        
        [Inject]
        private IOverheadEntryRepository OverheadEntryRepository { get; set; }
        
        [Inject]
        private IOverheadEntryChangelogRepository OverheadEntryChangelogRepository { get; set; }
        
        [Inject]
        private ISprintRepository SprintRepository { get; set; }
        
        [Inject]
        private ILogger<EditOverheadEntry> Logger { get; set; }
        
        [Parameter]
        public OverheadEntry Entry { get; set; }
        
        [Parameter]
        public EventCallback OnClose { get; set; }

        private bool IsNewOverheadEntry => Entry.Id == default;

        private bool _concurrencyError;

        private EditContext _editContext;

        private OverheadEntryForm _model;
        
        private List<OverheadSession> _overheadSessions = new();

        private Sprint _sprint;
        
        private bool _isCurrentlySubmitting = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (IsNewOverheadEntry) {                               
                _model = new OverheadEntryForm();                                         
            } else {
                _model = new OverheadEntryForm(Entry);                                 
            }            
            _editContext = new(_model);
            _overheadSessions =
                await OverheadSessionRepository.GetAllAsync(query => query.OrderBy(session => session.Name));

            _sprint = Entry.Sprint ?? await SprintRepository.GetByIdAsync(Entry.SprintId);
        }

        /// <summary> 
        /// Calls the savechanges method and if saving is successful invokes the OnClose EventCallback.
        /// </summary>
        /// <returns>A Task</returns>
        private async Task OnFormSubmit()
        {
            if (_isCurrentlySubmitting) {return;}
            _isCurrentlySubmitting = true;
            var success = await SaveChanges();
            if (success) await OnClose.InvokeAsync();
            _isCurrentlySubmitting = false;
        }

        protected override bool Validate() => _editContext.Validate();

        /// <summary> 
        /// Tries to persist the overhead entry in the database. If a concurrency error occurrs, an error message will be displayed.
        /// </summary>
        /// <returns>A Task containing a boolean. True if saving was successful without errors.</returns>
        private async Task<bool> SaveChanges()
        {
            _concurrencyError = false;
            var changes = _model.ApplyChanges(Self, Entry);

            if (IsNewOverheadEntry)
            {
                Entry.Created = DateTime.Now;
                Entry.User = Self;
            }

            var savedEntry = Entry.CloneForPersisting();
            if (IsNewOverheadEntry) {
                await OverheadEntryRepository.AddAsync(savedEntry);
            }
            else
            {
                try {               
                    await OverheadEntryRepository.UpdateAsync(savedEntry);
                } catch (DbUpdateConcurrencyException ex) {                    
                    Logger.LogInformation($"Update failed for worklog entry: (Id={savedEntry.Id}). Concurrency exception occurred: {ex.Message}");               
                    _concurrencyError = true;
                    return false;
                }  
                await OverheadEntryChangelogRepository.AddAllAsync(changes);
            }
            // Propagate changes back
            Entry.Id = savedEntry.Id;
            Entry.RowVersion = savedEntry.RowVersion;
            return true;
        }

        protected override async Task SubmitForm()
        {
            await SaveChanges();
        }
    }
}
