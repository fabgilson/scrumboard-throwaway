using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Forms;
using ScrumBoard.Services;

namespace ScrumBoard.Shared.Inputs;

public partial class TaggedWorkInstanceInput
{
    [Inject] public IWorklogTagService WorklogTagService { get; set; }

    [Parameter] public ICollection<TaggedWorkInstanceForm> TaggedWorkInstanceForms { get; set; }

    [Parameter] public EventCallback<ICollection<TaggedWorkInstanceForm>> TaggedWorkInstanceFormsChanged { get; set; }

    private IEnumerable<WorklogTag> RemainingWorklogTags =>
        _worklogTags?.Where(tag => !TaggedWorkInstanceForms.Select(twi => twi.WorklogTagId).Contains(tag.Id)) ?? new List<WorklogTag>();

    private TaggedWorkInstanceForm _newTaggedWorkInstanceForm;
    private IEnumerable<WorklogTag> _worklogTags;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _newTaggedWorkInstanceForm = new TaggedWorkInstanceForm();
        TaggedWorkInstanceForms = new List<TaggedWorkInstanceForm>(TaggedWorkInstanceForms ?? new List<TaggedWorkInstanceForm>());
        _worklogTags = await WorklogTagService.GetAllAsync();
    }

    private async Task AddItem()
    {
        if (TaggedWorkInstanceForms.Any(x => x.WorklogTagId == _newTaggedWorkInstanceForm.WorklogTagId))
            return;
        TaggedWorkInstanceForms.Add(_newTaggedWorkInstanceForm);
        _newTaggedWorkInstanceForm = new TaggedWorkInstanceForm();
        await TaggedWorkInstanceFormsChanged.InvokeAsync(TaggedWorkInstanceForms);
    }

    private async Task RemoveItem(TaggedWorkInstanceForm item)
    {
        TaggedWorkInstanceForms.Remove(item);
        await TaggedWorkInstanceFormsChanged.InvokeAsync(TaggedWorkInstanceForms);
    }

    private WorklogTag GetTagById(long id)
    {
        return _worklogTags.FirstOrDefault(x => x.Id == id);
    }
}