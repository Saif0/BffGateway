using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;

namespace BffGateway.WebApi.Swagger;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();

            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                var enumName = enumValue.ToString();
                if (!string.IsNullOrEmpty(enumName))
                {
                    schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumName));
                }
            }

            schema.Type = "string";
            schema.Format = null;
        }
    }

    private static string GetEnumDescription(Type enumType, string enumValue)
    {
        var field = enumType.GetField(enumValue);
        if (field == null) return enumValue;

        var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;

        return attribute?.Description ?? enumValue;
    }
}
