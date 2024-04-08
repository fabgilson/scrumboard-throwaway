using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ScrumBoard.Models.Entities;

namespace ScrumBoard.Filters
{
    public class OverheadEntryFilter
    {
        private ICollection<OverheadSession> _sessionFilter = new List<OverheadSession>();
        public ICollection<OverheadSession> SessionFilter
        {
            get => _sessionFilter;
            set
            {
                _sessionFilter = value;
                OnUpdate?.Invoke();
            }
        }

        private ICollection<User> _userFilter = new List<User>();
        public ICollection<User> UserFilter
        {
            get => _userFilter;
            set
            {
                _userFilter = value;
                OnUpdate?.Invoke();
            }
        }

        public bool FilterEnabled => SessionFilter.Any() || UserFilter.Any();
        
        ///<summary>Event for when this filter is updated</summary>
        public event Action OnUpdate;

        /// <summary>
        /// A function passed into overhead page as a predicate. If for the given overhead entry returns true, 
        /// then the overhead entry will be included in the table, based on the filter contents. 
        /// Otherwise, the entry will not be included in the table.
        /// </summary>
        /// <param name="entry">Instance of overhead page entry to be filtered or not</param>
        /// <returns>true if the parameter passes all the filters, otherwise false</returns>
        public Expression<Func<OverheadEntry, bool>> Predicate {
            get
            {
                var userIds = UserFilter.Select(user => user.Id).ToList();
                var sessionIds = SessionFilter.Select(session => session.Id).ToList();
                return entry => 
                    (!userIds.Any() || userIds.Contains(entry.UserId)) && 
                    (!sessionIds.Any() || sessionIds.Contains(entry.SessionId));
            }
        }

        public void Clear() {
            UserFilter.Clear();
            SessionFilter.Clear();
            OnUpdate?.Invoke();
        }
    }
}

