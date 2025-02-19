// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Coherence.Editor
{
    public class StatusTrackerController : ScriptableSingleton<StatusTrackerController>
    {
        [SerializeField]
        private List<StatusTracker> trackers = new List<StatusTracker>();

        public List<StatusTracker> Trackers => trackers;

        internal void ClearTrackers()
        {
            trackers.Clear();
        }

        public enum UpdateHandler
        {
            Manual,
            HierarchyChanged,
            ProjectChanged,
            SelectionChanged,
            OnAssetsChanged,
            OnEnable,
            OnLoginStatusChange,
            OnBakeEnded
        }

        public void Remove(TrackerIssue tracker)
        {
            if (tracker == null)
            {
                return;
            }

            Remove(tracker.Identifier);
        }

        public void Remove(string trackerID)
        {
            _ = trackers.RemoveAll(w => w == null || w.Identifier == trackerID);
        }

        public string Add(StatusTracker tracker)
        {
            //Already in list. Relates to hubmodules list not being serialized correctly
            if (trackers.Any(x => x.Identifier.Equals(tracker.Identifier)))
            {
                Debug.LogWarning("Trying to add issuewizard with existing indentifier");
                return tracker.Identifier;
            }

            tracker.Init();

            trackers.Add(tracker);
            return tracker.Identifier;
        }

        public IEnumerable<StatusTracker> GetActiveTrackersForScope(string scopeID)
        {
            foreach (var tracker in trackers)
            {
                if (!tracker.Hidden && tracker.ScopeId.Equals(scopeID))
                {
                    yield return tracker;
                }
            }
        }

        internal void ShowGenericMenu(IEnumerable<StatusTracker> trackers, bool showSubmenu)
        {
            GenericMenu contextMenu = new GenericMenu
            {
                allowDuplicateNames = true
            };
            foreach (var statusTracker in trackers)
            {
                var item = (TrackerIssue)statusTracker;
                var content = new GUIContent(item.Message);
                var prefix = showSubmenu ? $"{item.ScopeId}/" : "";
                content.text = $"{prefix}{item.Message.text}";
                contextMenu.AddItem(content, false, () => item.Trigger());
            }

            contextMenu.ShowAsContext();
        }
    }
}
