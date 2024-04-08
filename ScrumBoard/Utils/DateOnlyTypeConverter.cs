using System;

namespace ScrumBoard.Utils
{
    public class DateOnlyTypeConverter : StringToTypeConverter<DateOnly> 
    {
        protected override DateOnly Decode(string input)
        {
            return DateOnly.Parse(input);
        }

        protected override string Encode(DateOnly input)
        {
            return input.ToString("O");
        }
    }
}