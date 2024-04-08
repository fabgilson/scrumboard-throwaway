using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharedLensResources.Blazor.Util;

public static class PageRoutingUtils
{
    private static readonly Type[] SupportedParamTypes = {
        typeof(int?),
        typeof(int),
        typeof(long),
        typeof(string),
    };

    public static string GenerateRelativeUrlWithParams(string template, string anchor, params (string, object)[] urlParameters)
    {
        if (urlParameters.Select(x => SupportedParamTypes.Contains(x.Item2.GetType())).Any(x => !x))
        {
            throw new ArgumentException(
                "Unsupported parameter type given, url parameters must be one of: " +
                string.Join(", ", SupportedParamTypes.Select(x => x.ToString()))
            );
        }
        
        var trimmed = template.TrimStart('.').TrimStart('/');
        var regex = new Regex("{(.*?)}");
        var templateParams = regex.Matches(trimmed);

        if (templateParams.Count != urlParameters.Length)
        {
            throw new InvalidOperationException(
                $"Incorrect number of parameters given, expected {templateParams.Count} but received {urlParameters.Length}"
            );
        }

        var parameterised = trimmed;
        parameterised = regex.Replace(parameterised, m => 
            urlParameters
                .First(x => 
                    x.Item2 is not string 
                        ? x.Item1 == m.Value.TrimStart('{').TrimEnd('}').TrimEnd('?').Split(':')[0]
                        : x.Item1 == m.Value.TrimStart('{').TrimEnd('}').TrimEnd('?')
                )
                .Item2.ToString()
        );

        return $"./{parameterised}{(anchor is null ? "" : "#")}{anchor?.TrimStart('#') ?? ""}";
    }
    
    public static string GenerateRelativeUrlWithParams(string template, params (string, object)[] urlParameters)
    {
        return GenerateRelativeUrlWithParams(template, null, urlParameters);
    }
}