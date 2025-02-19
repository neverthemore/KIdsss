using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Coherence.Editor
{
    public static class MessageQueue
    {
        private static Dictionary<string, Dictionary<string, Action>> messages = new Dictionary<string, Dictionary<string, Action>>();

        public static bool HasMessages(string scopeID)
        {
            return GetMessageActions(scopeID).Count >0;
        }

        public static void AddToQueue(string scopeID, EditorTask task, Action action)
        {
            var messageDict = GetMessageDict(scopeID);
            if (!messageDict.TryGetValue(task.TaskID, out _))
            {
                messageDict.Add(task.TaskID, action);
                task.OnTaskCompleted += TaskCompleted;
            }
        }

        private static void TaskCompleted(EditorTask task)
        {
            task.OnTaskCompleted -= TaskCompleted;
            foreach (var kvPair in messages.Values)
            {
                if (kvPair.ContainsKey(task.TaskID))
                    kvPair.Remove(task.TaskID);
            }
        }

        public static void ProcessActiveMessages(string scopeID)
        {
            foreach (var item in GetMessageActions(scopeID))
            {
                item.Invoke();
            }
        }

        private static List<Action> GetMessageActions(string scopeID)
        {
            return GetMessageDict(scopeID).Values.ToList();
        }

        private static Dictionary<string, Action> GetMessageDict(string scopeID)
        {
            if (messages.TryGetValue(scopeID, out Dictionary<string, Action> dict))
            {
                return dict;
            }

            var newDict = new Dictionary<string, Action>();
            messages.Add(scopeID, newDict);
            return newDict;
        }

        internal static void AddToQueue(GUIContent message)
        {
            AddToQueue(StatusTrackerConstructor.Scopes.ProjectStatus,
                EditorTasks.StartTask(Guid.NewGuid().ToString()),
                () => CoherenceHubLayout.DrawLabel(message));
        }
    }
}
