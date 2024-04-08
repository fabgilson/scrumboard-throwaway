using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using ScrumBoard.Filters;
using ScrumBoard.Models.Entities;
using Xunit;

namespace ScrumBoard.Tests.Unit.Filters
{
    public class OverheadEntryFilterTest
    {
        private readonly User _user1;
        
        private readonly User _user2;

        private readonly OverheadSession _session1;
        private readonly OverheadSession _session2;

        private readonly OverheadEntryFilter _filter;

        private readonly OverheadEntry _session1User1;
        private readonly OverheadEntry _session1User2;
        private readonly OverheadEntry _session2User1;
        private readonly OverheadEntry _session2User2;
        
        private readonly List<OverheadEntry> _entries = new();

        public OverheadEntryFilterTest()
        {
            _user1 = new User()
            {
                Id = 1,
            };
            _user2 = new User()
            {
                Id = 2,
            };

            _session1 = new OverheadSession()
            {
                Id = 11,
            };
            
            _session2 = new OverheadSession()
            {
                Id = 12,
            };

            _session1User1 = new OverheadEntry()
            {
                Session = _session1,
                User = _user1,
            };
            _session2User1 = new OverheadEntry()
            {
                Session = _session2,
                User = _user1,
            };
            _session1User2 = new OverheadEntry()
            {
                Session = _session1,
                User = _user2,
            };
            _session2User2 = new OverheadEntry()
            {
                Session = _session2,
                User = _user2,
            };
            
            _entries.AddRange(new []
            {
                _session1User1, 
                _session1User2,
                _session2User1,
                _session2User2,
            });
            
            foreach (var entry in _entries)
            {
                entry.SessionId = entry.Session.Id;
                entry.UserId = entry.User.Id;
            }
            

            _filter = new();
        }
        
        /// <summary>
        /// Runs the filter on _entries
        /// </summary>
        /// <returns>List of matching entries</returns>
        private List<OverheadEntry> RunFilter()
        {
            var predicate = _filter.Predicate.Compile();
            return _entries.Where(predicate).ToList();
        }

        [Fact]
        public void SetUserFilter_AnyUsers_OnUpdateTriggered()
        {
            Mock<Action> action = new(MockBehavior.Strict);
            _filter.OnUpdate += action.Object;
            
            action.Setup(mock => mock());
            _filter.UserFilter = new List<User>();
            action.Verify(mock => mock(), Times.Once);
        }
        
        [Fact]
        public void SetSessionFilter_AnySessions_OnUpdateTriggered()
        {
            Mock<Action> action = new(MockBehavior.Strict);
            _filter.OnUpdate += action.Object;
            
            action.Setup(mock => mock());
            _filter.SessionFilter = new List<OverheadSession>();
            action.Verify(mock => mock(), Times.Once);
        }

        [Fact]
        public void Clear_Called_OnUpdateTriggered()
        {
            Mock<Action> action = new(MockBehavior.Strict);
            _filter.OnUpdate += action.Object;
            
            action.Setup(mock => mock());
            _filter.Clear();
            action.Verify(mock => mock(), Times.Once);
        }

        [Fact]
        public void EmptyFilter_AllEntriesReturned()
        {
            var matching = RunFilter();
            matching.Should().BeEquivalentTo(_entries);
        }

        [Fact]
        public void UserFilter_MatchesExpectedEntries()
        {
            _filter.UserFilter = new List<User> {_user1};

            var matching = RunFilter();
            matching.Should().BeEquivalentTo(new[] { _session1User1, _session2User1 });
        }
        
        [Fact]
        public void SessionFilter_MatchesExpectedEntries()
        {
            _filter.SessionFilter = new List<OverheadSession> {_session1};

            var matching = RunFilter();
            matching.Should().BeEquivalentTo(new[] { _session1User1, _session1User2 });
        }
        
        [Fact]
        public void UserAndSessionFilter_MatchesExpectedEntry()
        {
            _filter.UserFilter = new List<User> {_user1};
            _filter.SessionFilter = new List<OverheadSession> {_session1};

            var matching = RunFilter();
            matching.Should().BeEquivalentTo(new[] { _session1User1 });
        }
    }
}
