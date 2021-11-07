using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GoXLR.Server.Models
{
    public static class EnumExtensions
    {
        public static string[] GetAllEnumDescription<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Select(v => v.GetEnumDescription())
                .ToArray();
        }

        public static string GetEnumDescription<TEnum>(this TEnum value)
            where TEnum : struct, Enum
        {
            var enumType = typeof(TEnum);

            var fieldName = value.ToString();
            var fieldInfo = enumType.GetField(fieldName);

            var attribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>(false);

            return attribute?.Description
                   ?? throw new InvalidOperationException($"Enum '{enumType.Name}' with value '{value}' is missing the 'DescriptionAttribute'"); ;
        }
    }
}
