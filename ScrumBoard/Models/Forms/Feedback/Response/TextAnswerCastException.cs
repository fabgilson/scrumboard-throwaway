using System;
using ScrumBoard.Services;

namespace ScrumBoard.Models.Forms.Feedback.Response;

public class TextAnswerCastException: Exception
{
    public TextAnswerCastException() : base(
        $"Could not cast to TextAnswer. A non-text answer may have been passed to the form.")
    {
    }
}