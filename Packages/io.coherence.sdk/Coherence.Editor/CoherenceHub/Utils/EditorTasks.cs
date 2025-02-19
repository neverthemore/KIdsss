using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Coherence.Editor
{
    public static class EditorTasks
    {
        private const int defaultFeedbackTime = 3500;
        private static Dictionary<string, EditorTask> taskDict = new Dictionary<string, EditorTask>();

        internal static bool TryGetEditorTask(string taskID, out EditorTask task)
        {
            return taskDict.TryGetValue(taskID, out task);
        }

        internal static EditorTask StartTask(string taskID, int msTime = defaultFeedbackTime)
        {
            if (taskDict.TryGetValue(taskID, out EditorTask task))
            {
                task.ResetTime(msTime);
                return task;
            }
            else
            {
                var timerTask = new EditorTask(taskID, msTime, () => TimerStart(taskID));
                taskDict.Add(taskID, timerTask);
                timerTask.Start();
                return timerTask;
            }
        }

        private static async void TimerStart(string taskID)
        {
            while (!taskDict[taskID].HasTimedOut())
            {
                await Task.Delay(100);
            }

            taskDict[taskID].Complete();
            taskDict.Remove(taskID);
        }
    }

    public class EditorTask : Task
    {
        public string TaskID { get; private set; }
        DateTime StartTime;
        DateTime EndTime;
        public Action<EditorTask> OnTaskCompleted { get; set; }

        public double Completion => (DateTime.Now - StartTime).TotalMilliseconds / (EndTime - StartTime).TotalMilliseconds;


        public EditorTask(string taskID, int msTime, Action action) : base(action)
        {
            TaskID = taskID;
            ResetTime(msTime);
        }

        internal bool HasTimedOut()
        {
            EditorApplication.update -= RepaintAllHubWindows;
            EditorApplication.update += RepaintAllHubWindows;
            return EndTime < DateTime.Now;
        }

        internal void Complete()
        {
            EditorApplication.update -= RepaintAllHubWindows;
            EditorApplication.update += RepaintAllHubWindows;
            OnTaskCompleted?.Invoke(this);
        }

        internal void ResetTime(int msTime)
        {
            StartTime = DateTime.Now;
            EndTime = StartTime.AddMilliseconds(msTime);
        }

        /// <summary>
        /// Makes sure that all HubModuleWindows paint messages
        /// Called through EditorApplication.update because it has to happens on main thread
        /// </summary>
        public void RepaintAllHubWindows()
        {
            EditorApplication.update -= RepaintAllHubWindows;
            Resources.FindObjectsOfTypeAll<BaseModuleWindow>().ToList().ForEach(x => x.Repaint());
        }
    }
}
