using System;

namespace ScrumBoard.Utils
{
    public static class LanguageUtils
    {
        public static string AsShortString(DateTime dateTime) {
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            if (currentDate == DateOnly.FromDateTime(dateTime)) {
                return dateTime.ToShortTimeString();
            } else {
                return dateTime.ToShortDateString();
            }
        }

        public static string PluraliseNoun(string noun, int quantity)
        {
            return PluraliseNoun(noun, noun + "s", quantity);
        }

        public static string PluraliseNoun(string noun, string pluralForm, int quantity)
        {
            if (quantity == 1) {
                return quantity + " " + noun;
            } else {
                return quantity + " " + pluralForm;
            }
        }

        public static string StripNewLines(string inputString) {
            return inputString.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", "");
        }
    }
}