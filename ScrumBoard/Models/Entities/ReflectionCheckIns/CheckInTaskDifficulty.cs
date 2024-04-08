using System.ComponentModel;

namespace ScrumBoard.Models.Entities.ReflectionCheckIns;

public enum CheckInTaskDifficulty
{
    None = 0,
    
    [Description("very easy")]
    VeryEasy = 1,
    
    [Description("easy")]
    Easy = 2,
    
    [Description("not easy, but not hard either")]
    Medium = 3,
    
    [Description("hard")]
    Hard = 4,
    
    [Description("very hard")]
    VeryHard = 5
}