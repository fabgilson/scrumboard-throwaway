using System;

namespace ScrumBoard.Utils
{
    public enum ChangeType {
        Create,
        Update,
        Delete,        
    }

    public struct Change<T> {

        public static Change<T> Create(T value)
        {
            return new Change<T>(ChangeType.Create, default, value);
        }

        public static Change<T> Update(T oldValue, T newValue)
        {
            return new Change<T>(ChangeType.Update, oldValue, newValue);
        }

        public static Change<T> Delete(T value)
        {
            return new Change<T>(ChangeType.Delete, value, default);
        }

        /// <summary>
        /// Generates a change if one exists from a original value to a new value
        /// </summary>
        /// <param name="original">Initial value</param>
        /// <param name="updated">Updated value</param>
        /// <returns>Change between the two values, if values are the same then null is returned</returns>
        public static Change<T>? Generate(T original, T updated)
        {
            if (Equals(original, updated)) return null;
            if (updated == null) return Delete(original);
            if (original == null) return Create(updated);

            return Update(original, updated);
        }
        

        private Change(ChangeType type, T fromValue, T toValue)
        {
            Type = type;
            FromValue = fromValue;
            ToValue = toValue;
        }

        public Change<TResult> Cast<TResult>()
        {
            return new Change<TResult>(Type, (TResult)(object)FromValue, (TResult)(object)ToValue);
        }

        public ChangeType Type { get; }
        public T FromValue { get; }
        public T ToValue { get; }
    }
}