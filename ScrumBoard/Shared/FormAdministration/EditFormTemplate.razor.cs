using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Models.Forms.Feedback;
using ScrumBoard.Models.Forms.Feedback.TemplateBlocks;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.FormAdministration;

public partial class EditFormTemplate
{
    [CascadingParameter(Name = "Self")]
    public User Self { get; set; }
    
    [Parameter]
    public FormTemplate FormTemplate { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }
    
    [Parameter]
    public EventCallback<FormTemplate> OnPreview { get; set; }

    [Parameter]
    public EventCallback<FormTemplate> OnSave { get; set; }
    
    [Inject]
    private IFormTemplateService FormTemplateService { get; set; }
    
    private bool _isCurrentlySubmitting = false;

    private readonly List<Func<FormTemplateBlockForm>> BlockTypes = new()
    {
        () => new TextBlockForm(),
        () => new PageBreakForm(),
        () => new TextQuestionForm(),
        () => new MultiChoiceQuestionForm() { Options = new List<MultichoiceOptionForm>() { new(), new() }},
    };
    
    private FormTemplate _previewedFormTemplate;

    private EditContext _editContext;
    
    private bool IsNewForm => FormTemplate.Id == default;

    private FormTemplateForm _model;
    
    private bool _concurrentUpdate;
    
    

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _model = IsNewForm ? new FormTemplateForm() : new FormTemplateForm(FormTemplate);
        _editContext = new EditContext(_model);
    }


    private static Dictionary<string, object> CreateDictionaryFromBlock(FormTemplateBlockForm block)
    {
        return new Dictionary<string, object>{{ "FormBlockForm", block }};
    }

    /// <summary> 
    /// Adds the given form block to the model.
    /// </summary>
    /// <param name="form">A FormBlockForm to add.</param>
    private void AddBlock(FormTemplateBlockForm form)
    {
        _model.Blocks.Add(form);
    }

    /// <summary> 
    /// Removed a block from the model by its index.
    /// </summary>
    /// <param name="index">The index of a FormBlockForm to remove</param>
    private void RemoveBlock(int index)
    {
        _model.Blocks.RemoveAt(index);
    }

    private void SwapBlocks(int firstIndex, int secondIndex)
    {
        (_model.Blocks[firstIndex], _model.Blocks[secondIndex]) = (_model.Blocks[secondIndex], _model.Blocks[firstIndex]);
    }

    private void MoveUp(int currentIndex)
    {
        if (currentIndex <= 0) {return;}
        SwapBlocks(currentIndex - 1, currentIndex);
    }

    private void MoveDown(int currentIndex)
    {
        if (currentIndex >= _model.Blocks.Count - 1) {return;}
        SwapBlocks(currentIndex, currentIndex + 1);
    }

    /// <summary> 
    /// Marks the current form name as a duplicate and validates the model.
    /// </summary>
    private void MarkNameDuplicate()
    {
        _model.DuplicateName = true;
        _editContext.NotifyFieldChanged(new FieldIdentifier(_model, nameof(FormTemplateForm.Name)));
        _editContext.Validate();
    }
    /// <summary> 
    /// Creates and returns a new FeedBackForm with the name and blocks from the current model.
    /// </summary>
    /// <returns>A newly created FeedBackForm</returns>
    private FormTemplate CreateNewForm()
    {
        var blocks = _model.Blocks.Select(block => block.AsEntity).ToList();

        var index = 0;
        foreach (var block in blocks)
        {
            block.FormPosition = index;
            index++;
        }
        
        var newFormTemplate =  new FormTemplate
        {
            Name = (_model.Name ?? "").Trim(),
            Blocks = blocks
        };

        if (IsNewForm)
        {
            newFormTemplate.CreatorId = Self.Id;
            newFormTemplate.Created = DateTime.Now;
        }
        else
        {
            newFormTemplate.Id = FormTemplate.Id;
            newFormTemplate.CreatorId = FormTemplate.CreatorId;
            newFormTemplate.Created = FormTemplate.Created;
            newFormTemplate.RowVersion = FormTemplate.RowVersion;
            foreach (var block in newFormTemplate.Blocks) block.FormTemplateId = newFormTemplate.Id;
            foreach (var block in newFormTemplate.Blocks.OfType<MultiChoiceQuestion>())
            {
                foreach (var option in block.Options) option.BlockId = block.Id;
            }
        }

        return newFormTemplate;
    }

    private async Task OnSubmit()
    {
        if (_isCurrentlySubmitting) {return;}
        _isCurrentlySubmitting = true;
        await Submit();
        _isCurrentlySubmitting = false;
    }
    

    /// <summary> 
    /// Saves a new, or updates the exisiting feedbackform in the database.
    /// </summary>
    /// <returns>A Task</returns>
    private async Task Submit()
    {
        _concurrentUpdate = false;
        var newFormTemplate = CreateNewForm();
        var nameValid = await FormTemplateService.CheckForDuplicateName(newFormTemplate.Name, FormTemplate.Name);

        if (nameValid)
        {
            MarkNameDuplicate(); ;
            return;
        }
        
        try
        {
            await FormTemplateService.AddOrUpdateAsync(newFormTemplate);
            await OnSave.InvokeAsync(newFormTemplate);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _concurrentUpdate = true;
            Logger.LogInformation($"Update failed for feedback form (name={newFormTemplate.Name}). Concurrency exception occurred: {ex.Message}");
        }


    }

    /// <summary> 
    /// Opens a preview of the current form with the existing data from the model.
    /// </summary>
    private void OpenPreview()
    {
        _previewedFormTemplate = CreateNewForm();
        _previewedFormTemplate.Id = FormTemplate.Id;
        OnPreview.InvokeAsync(_previewedFormTemplate);
    }
}