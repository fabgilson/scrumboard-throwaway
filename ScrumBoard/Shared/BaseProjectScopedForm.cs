
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Services;

namespace ScrumBoard.Shared
{
    /// <summary> 
    /// Convenient base class for any form that needs to be saved before the current project changes.
    /// </summary>
    public abstract class BaseProjectScopedForm : ComponentBase
    {
        [CascadingParameter(Name = "ProjectState")]
        public ProjectState ProjectState { get; set; }

        private bool _alreadyInitialised = false;

        protected override void OnInitialized()
        {
            if (_alreadyInitialised) throw new InvalidOperationException("Cannot init ProjectScopedForm multiple times");
            _alreadyInitialised = true;
            base.OnInitialized();
        }
        
        /// <summary> 
        /// Abstract validation method for the inherited form. This method is repsonsible for validating all form fields.
        /// </summary>
        protected abstract bool Validate();

        /// <summary> 
        /// Abstract submit method for the inherited form. This method is responsible for saving all form changes.
        /// </summary>
        protected abstract Task SubmitForm();
    }
}