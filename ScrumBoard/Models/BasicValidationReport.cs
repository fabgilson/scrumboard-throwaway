using System.Collections.Generic;
using System.Linq;

namespace ScrumBoard.Models;

public struct BasicValidationError
{
    public string MemberName { get; set; }
    public string ErrorText { get; set; }
}

public class BasicValidationReport
{
    private readonly IList<BasicValidationError> _validationErrors = new List<BasicValidationError>();
    public IEnumerable<BasicValidationError> ValidationErrors => _validationErrors;

    public void AddValidationError(string memberName, string errorMessage)
    {
        _validationErrors.Add(new BasicValidationError { MemberName = memberName, ErrorText = errorMessage});
    }
    
    public bool Success => !ValidationErrors.Any();
}