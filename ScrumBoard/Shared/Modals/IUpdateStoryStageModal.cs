using System.Threading.Tasks;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Shared.Modals
{
    public interface IUpdateStoryStageModal
    {  
        /// <summary> Updates user story stage given the tasks new stage </summary>
        /// <param name="task"> Task that the stage is being applied to </param>
        /// <param name="newStage"> New stage for the task </param>
        /// <returns> Whether the task operation has been cancelled by the user </returns>
        Task<bool> Show(UserStoryTask task, Stage newStage);
    }
}