using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SharedLensResources;

namespace LensCoreDashboard.Shared;

public partial class CreateUser
{
    [Inject]
    protected LensAuthenticationService.LensAuthenticationServiceClient LensAuthenticationServiceClient { get; set; }
    
    private CreateNewLensAccountRequest _newUserRequest;

    private EditContext _editContext;
    private ValidationMessageStore _validationMessageStore;
    private string _successMessage;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        InitialiseNewForm();
    }

    private void InitialiseNewForm(string successMessage = null)
    {
        _newUserRequest = new CreateNewLensAccountRequest();
        _editContext = new EditContext(_newUserRequest);
        _validationMessageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += (s, e) => _validationMessageStore.Clear();
        _editContext.OnFieldChanged += (s, e) => _validationMessageStore.Clear(e.FieldIdentifier);
        _successMessage = successMessage;
    }

    private async Task OnValidSubmitAsync()
    {
        var response = await LensAuthenticationServiceClient.CreateNewLensAccountAsync(_newUserRequest);
        if (response.Validation.IsSuccess) InitialiseNewForm("Account created successfully!");

        foreach (var validationError in response.Validation.ValidationErrors)
        {
            foreach (var fieldName in validationError.FieldNames)
            {
                _validationMessageStore.Add(
                    new FieldIdentifier(_newUserRequest, fieldName),
                    validationError.ErrorText
                );
            }
        }
        _editContext.NotifyValidationStateChanged();
    }
}