using System;
using System.Text.Json;
using System.Linq;

namespace ScrumBoard.Utils
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return string.Concat(
                name.SelectMany(c => char.IsUpper(c) ? $"_{char.ToLower(c)}" : $"{c}")
            ).TrimStart('_');
        }
    }
}