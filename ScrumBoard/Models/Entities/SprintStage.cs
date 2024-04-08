using System;

namespace ScrumBoard.Models.Entities 
{
    public enum SprintStage 
    {
        Created,
        Started,
        ReadyToReview,
        InReview,
        Reviewed,
        Closed,
    }

    public static class SprintStageExtensions
    {
        /// <summary>
        /// Returns whether a sprint with this stage has all of its work done
        /// </summary>
        /// <returns>Whether stage is for a done sprint</returns>
        public static bool IsWorkDone(this SprintStage stage)
        {
            switch (stage)
            {
                case SprintStage.ReadyToReview:
                case SprintStage.InReview:
                case SprintStage.Reviewed:
                case SprintStage.Closed:
                    return true;
                case SprintStage.Created:
                case SprintStage.Started:
                    return false;
                default:
                    throw new NotSupportedException($"Unknown sprint stage: {stage}");
            }
        }
    }
}