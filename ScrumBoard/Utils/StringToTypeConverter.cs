using System;
using System.ComponentModel;
using System.Globalization;

namespace ScrumBoard.Utils
{
    public abstract class StringToTypeConverter<T> : TypeConverter 
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str) return Decode(str);
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object obj, Type destinationType)
        {
            if (destinationType == typeof(string) && obj is T value) return Encode(value);
            return base.ConvertTo(context, culture, obj, destinationType);
        }

        protected abstract string Encode(T input);

        protected abstract T Decode(string input);
    }
}