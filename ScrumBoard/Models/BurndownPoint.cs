using System;
using System.Text.Json.Serialization;
using ScrumBoard.Models.Entities;
using ScrumBoard.Models.Entities.Changelog;

namespace ScrumBoard.Models
{
    public enum BurndownPointType {
        None,
        Worklog,
        ScopeChange,
        NewTask,
        StageChange,
    }

    public class BurndownPoint<T>
    {
        /// <summary> Constructor required for JSON deserialisation </summary>
        [JsonConstructor]
        public BurndownPoint(DateTime moment, T value, BurndownPointType type, long id) {
            Moment = moment;
            Value = value;
            Type = type;
            Id = id;
        }

        /// <summary> Creates a new BurndownPoint with no associated entity </summary>
        public static BurndownPoint<T> Initial(DateTime moment, T value)
        {
            return new(
                moment,
                value,
                BurndownPointType.None,
                default
            );
        }

        /// <summary> Creates a new BurndownPoint corresponding to a new user story task </summary>
        public static BurndownPoint<T> NewTask(DateTime moment, T value, UserStoryTask task)
        {
            return new(
                moment,
                value,
                BurndownPointType.NewTask,
                task.Id
            );
        }

        /// <summary> Creates a new BurndownPoint corresponding to a scope change in a user story task </summary>
        public static BurndownPoint<T> ScopeChange(DateTime moment, T value, UserStoryTaskChangelogEntry change)
        {
            if (change.FieldChanged != nameof(UserStoryTask.Estimate)) 
                throw new InvalidOperationException("Cannot create ScopeChange BurndownPoint from non-scope ChangelogEntry");
            return new(
                moment,
                value,
                BurndownPointType.ScopeChange,
                change.Id
            );
        }
        
        /// <summary> Creates a new BurndownPoint corresponding to a stage change in a user story task </summary>
        public static BurndownPoint<T> StageChange(DateTime moment, T value, UserStoryTaskChangelogEntry change)
        {
            if (change.FieldChanged != nameof(UserStoryTask.Stage)) 
                throw new InvalidOperationException("Cannot create StageChange BurndownPoint from non-stage ChangelogEntry");
            return new(
                moment,
                value,
                BurndownPointType.StageChange,
                change.Id
            );
        }

        /// <summary> Creates a new BurndownPoint corresponding to a worklog entry </summary>
        public static BurndownPoint<T> Worklog(DateTime moment, T value, WorklogEntry entry)
        {
            return new(
                moment,
                value,
                BurndownPointType.Worklog,
                entry.Id
            );
        }

        /// <summary> Returns a new BurndownPoint with all properties the same except for an updated value </summary>
        public BurndownPoint<TDest> WithValue<TDest>(TDest value) {
            return new BurndownPoint<TDest>(Moment, value, Type, Id);
        }

        [JsonPropertyName("x")]
        public DateTime Moment { get; private set; }

        [JsonPropertyName("y")]
        public T Value { get; private set; }

        /// <summary> Type of the change associated with this BurndownPoint </summary>
        public BurndownPointType Type { get; private set; }

        /// <summary> Id of the entity associated with this BurndownPoint or default if Type == None </summary>
        public long Id { get; private set; }

        public override string ToString()
        {
            return $"BurndownPoint(Id={Id}, Moment={Moment}, Value={Value}, Type={Type})";
        }
    }
}