using System.Text.Json;
using ScrumBoard.Models.Entities.Relationships;

namespace ScrumBoard.Utils;

public class TaggedWorkInstanceTypeConverter: StringToTypeConverter<TaggedWorkInstance> 
{
    protected override TaggedWorkInstance Decode(string input)
    {
        return JsonSerializer.Deserialize<TaggedWorkInstance>(input);
    }

    protected override string Encode(TaggedWorkInstance input)
    {
        return JsonSerializer.Serialize(input);
    }
}