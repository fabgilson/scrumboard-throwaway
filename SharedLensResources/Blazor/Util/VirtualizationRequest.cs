using System.Collections.Generic;

namespace SharedLensResources.Blazor.Util;

public class VirtualizationRequest<T> where T : class
{
    public string SearchQuery { get; set; }
    
    public int StartIndex { get; set; }
    
    public int Count { get; set; }
    
    public IEnumerable<T> Excluded { get; set; }
}