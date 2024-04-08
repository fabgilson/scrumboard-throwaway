using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Entities.Forms.Instances;

public class MultiChoiceAnswer : Answer
{
    /// <summary>
    /// This is a many-to-many relationship between MultichoiceOption and this entity.
    /// This allows this entity to store which option(s) are selected.
    /// </summary>
    public ICollection<MultichoiceAnswerMultichoiceOption> SelectedOptions { get; set; }
}