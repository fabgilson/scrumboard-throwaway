using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models;

namespace ScrumBoard.Services
{

    ///<summary>Interface for the SortableList component that can easily be mocked</summary>
    public interface ISortableList<TItem> 
    {
        List<TItem> Items { get; }
        string Group { get; }
        Task TriggerItemsChanged(List<TItem> items);
        Task TriggerItemAdded(TItem item);
        void Synchronize();
    }

    public interface ISortableService<T>
    {
        /// <summary>Registers a new sortable list to this sortable service</summary>
        int Register(ISortableList<T> list);

        /// <summary>Unregisters a sortable list that is being disposed</summary>
        void Unregister(int key);

        /// <summary>Handles a sortable event and propagates the changes into the contents of the involved sortable lists</summary>
        /// <param name="args">Sortable event being handled containing the lists involved and where the item has been moved within/between the lists<param>
        Task HandleEvent(SortableEventArgs args);

        /// <summary>Gets the full group key from combining the type key and the sortable list's group <summary>
        string GetGroupKey(string subGroup);

        /// <summary>Notify all sortable lists in a certain group that their content is not what the group expects</summary>
        void SynchronizeGroup(string group);

        void SetSynchroniseOnMove(string group, bool shouldSync);
    }

    public class SortableService<T> : ISortableService<T>
    {
        private static int _nextTypeKey = 1; // Starting at 1 to avoid default

        private int _nextKey = 1; // Starting at 1 to avoid default

        private Dictionary<int, ISortableList<T>> _liveLists = new();

        private ILogger<SortableService<T>> _logger;

        private readonly int _typeKey;

        // Dictionary of group names and if requires SynchroniseGroup to be called whenever it changes
        private Dictionary<string, bool> _onMoveSync = new();

        public void SetSynchroniseOnMove(string group, bool shouldSync)
        {
            _onMoveSync[group] = shouldSync;
        }

        public SortableService(ILogger<SortableService<T>> logger) {
            _typeKey = _nextTypeKey++;
            _logger = logger;
            _logger.LogInformation($"Creating new SortableService (TypeKey={_typeKey})");
        }   
        
        ///<summary>Handles a sortable event and propagates the changes into the contents of the involved sortable lists</summary>
        ///<param name="args">Sortable event being handled containing the lists involved and where the item has been moved within/between the lists<param>
        public async Task HandleEvent(SortableEventArgs args)
        {
            var startList = _liveLists[args.From];
            _logger.LogDebug($"Moving object (TypeKey={_typeKey}, GroupKey={startList.Group}, Key={args.From}->{args.To}, Index={args.OldIndex}->{args.NewIndex})");
            var movedItem = startList.Items[args.OldIndex];
            
            var startItems = new List<T>(startList.Items);
            startItems.RemoveAt(args.OldIndex);

            if (args.From == args.To) { // Moving object withing same list
                if (args.OldIndex == args.NewIndex) return;

                startItems.Insert(args.NewIndex, movedItem);
                await startList.TriggerItemsChanged(startItems);

            } else { // Moving object between lists
                var endList = _liveLists[args.To];
                
                if (startList.Group != endList.Group) {
                    throw new InvalidOperationException("Invalid transfer between groups");
                }

                var endItems = new List<T>(endList.Items);
                endItems.Insert(args.NewIndex, movedItem);

                await startList.TriggerItemsChanged(startItems);
                await endList.TriggerItemsChanged(endItems);
                await endList.TriggerItemAdded(movedItem);

                if (startList.Group != null && _onMoveSync.ContainsKey(startList.Group) && _onMoveSync[startList.Group])
                {
                    SynchronizeGroup(startList.Group);
                }
            }
        }

        ///<summary>Registers a new sortable list to this sortable service</summary>
        public int Register(ISortableList<T> list)
        {
            var key = _nextKey++;
            _liveLists.Add(key, list);
            _logger.LogDebug($"Registering new SortableList (Key={key}, TypeKey={_typeKey})");
            return key;
        }

        ///<summary>Unregisters a sortable list that is being disposed</summary>
        public void Unregister(int key)
        {
            _logger.LogDebug($"Unregistering SortableList (Key={key}, TypeKey={_typeKey})");
            var list = _liveLists.GetValueOrDefault(key);
            if (list == null) {
                _logger.LogWarning($"Tried to unregister non-existant sortable list (key={key})");
            }
            else
            {
                _liveLists.Remove(key);
                SynchronizeGroup(list.Group);
            }
        }

        ///<summary>Gets the full group key from combining the type key and the sortable list's group<summary>
        public string GetGroupKey(string subGroup)
        {
            return $"{_typeKey}-{subGroup}";
        }

        public void SynchronizeGroup(string group)
        {
            foreach (var list in _liveLists.Values) {
                if (list.Group == group) {
                    list.Synchronize();
                }
            }
        }
    }
}