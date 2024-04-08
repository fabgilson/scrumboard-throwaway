using System;
using ScrumBoard.Models.Entities;
using ScrumBoard.Services;

namespace ScrumBoard.Models.Shapes
{
    public interface IProjectShape {
        string Name { get; set; }
        string Description { get; set; }
        DateOnly StartDate { get; set; }
        DateOnly EndDate { get; set; }
        GitlabCredentials GitlabCredentials { get; set; }
    }
}