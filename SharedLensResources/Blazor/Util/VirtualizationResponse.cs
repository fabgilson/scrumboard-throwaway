using System.Collections.Generic;

namespace SharedLensResources.Blazor.Util;

public class VirtualizationResponse<T> where T : class
{
    public IEnumerable<T> Results { get; set; }
    
    public int TotalPossibleResultCount { get; set; }
}