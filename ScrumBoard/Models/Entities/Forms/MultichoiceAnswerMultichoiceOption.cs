using System.Text.Json.Serialization;
using ScrumBoard.Models.Entities.Forms.Instances;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Models.Entities.Forms;

public class MultichoiceAnswerMultichoiceOption: IId
{
    public long Id { get; set; }

    public long MultichoiceAnswerId { get; set; }

    [JsonIgnore]
    public MultiChoiceAnswer MultichoiceAnswer { get; set; }

    public long MultichoiceOptionId { get; set; }

    [JsonIgnore]
    public MultiChoiceOption MultichoiceOption { get; set; }
}