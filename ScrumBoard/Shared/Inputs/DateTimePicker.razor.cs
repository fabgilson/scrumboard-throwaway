using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ScrumBoard.Shared.Inputs
{
    internal class DateTimePickerModel
    {
        private Action _onUpdate;
        
        public DateTimePickerModel(DateTime dateTime, Action onUpdate)
        {
            _time = TimeOnly.FromDateTime(dateTime);
            _date = DateOnly.FromDateTime(dateTime);
            _onUpdate = onUpdate;
        }

        private TimeOnly _time;

        public TimeOnly Time
        {
            get => _time;
            set
            {
                _time = value;
                _onUpdate();
            }
        }

        private DateOnly _date;
        public DateOnly Date
        {
            get => _date;
            set
            {
                _date = value;
                _onUpdate();
            }
        }
    }
    
    public partial class DateTimePicker : ComponentBase
    {
        [Parameter]
        public DateTime? Value { get; set; }

        [Parameter]
        public EventCallback<DateTime?> ValueChanged { get; set; }
        
        private EditContext InsideEditContext { get; set; }

        [Parameter]
        public string DateLabel { get; set; }

        [Parameter]
        public string TimeLabel { get; set; }
        
        /// <summary>
        /// By default, set the present to the earliest DateTime the picker will allow.
        /// Set this parameter allow the picker to be set to an earlier value.
        /// </summary>
        [Parameter]  
        public DateTime MinDateTime { get; set; } = DateTime.Now;

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> AdditionalAttributes { get; set; }
        
        private FieldIdentifier DateField => InsideEditContext.Field(nameof(_model.Date));
        private FieldIdentifier TimeField => InsideEditContext.Field(nameof(_model.Time));
        private string MinDateTimeString => MinDateTime.ToString("yyyy-MM-dd");

        private DateTimePickerModel _model;

        private void RefreshDateTime()
        {
            ValueChanged.InvokeAsync(_model.Date.ToDateTime(_model.Time));
            StateHasChanged();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (!firstRender) return;
            Value ??= DateTime.Now;
            _model = new DateTimePickerModel(Value.Value, RefreshDateTime);
            InsideEditContext = new EditContext(_model);
            RefreshDateTime();
        }
    }
}