using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public class TextAnswer : Answer
{
    public string Answer { get; set; }
}