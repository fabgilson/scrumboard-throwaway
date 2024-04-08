using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScrumBoard.Models.Entities.FeatureFlags;

/// <summary>
/// An entity that allows admins to dynamically enable certain features for a given project.
/// </summary>
public class ProjectFeatureFlag
{
    public long ProjectId { get; set; }
    
    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; }
    
    public FeatureFlagDefinition FeatureFlag { get; set; }
    
    public long? CreatorId { get; set; }
    
    [ForeignKey(nameof(CreatorId))]
    public User Creator { get; set; }
    
    public DateTime Created { get; set; }
}