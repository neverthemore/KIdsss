// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Editor.Portal;
    using Coherence.Editor.Toolkit;
    using Coherence.Log;
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using static Coherence.Editor.StatusTrackerController;
    using Logger = Coherence.Log.Logger;

    public class StatusTracker
    {
        public static class WarningContextMessages
        {
            public static readonly GUIContent IssueErrorIcon = EditorGUIUtility.IconContent("d_console.erroricon.sml");
            public static readonly GUIContent IssueWarningIcon = EditorGUIUtility.IconContent("Warning");
            public static readonly GUIContent IssueTickIcon = Icons.GetContent("Coherence.Tickbox", "Checklist of things to do to help setup coherence");
        }

        public enum Severity
        {
            Message,
            Warning,
            Error
        }

        public string ScopeId;
        public string Identifier;
        public Severity IssueSeverity;
        public GUIContent Message;
        protected Logger Logger = Log.GetLogger<StatusTracker>();

        protected int buttonAreaWidth = 140;

        public virtual bool Hidden => false;

        public StatusTracker(string identifier, string scopeId, Severity severity, GUIContent message)
        {
            this.Identifier = identifier;
            this.ScopeId = scopeId;
            this.IssueSeverity = severity;
            this.Message = message;
        }

        internal virtual void Init()
        {
        }

        protected GUIContent GetIssueIcon()
        {
            switch (IssueSeverity)
            {
                case Severity.Error:
                    return WarningContextMessages.IssueErrorIcon;
                case Severity.Warning:
                    return WarningContextMessages.IssueWarningIcon;
                default:
                    return WarningContextMessages.IssueTickIcon;
            }
        }

        internal void DrawMessageWithIcon(GUIContent content)
        {
            CoherenceHubLayout.DrawIconButton(GetIssueIcon());
            CoherenceHubLayout.DrawLabel(content);
        }

        internal virtual void Draw()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawMessageWithIcon(Message);
            }
        }
    }

    public class TrackerDashboardIssue : StatusTracker
    {
        public override bool Hidden => !string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID) ||
            !(!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken) && !PortalLogin.IsPolling);

        public TrackerDashboardIssue(string identifier, string scopeId, Severity severity, GUIContent message) : base(identifier, scopeId, severity, message)
        {
        }

        internal override void Draw()
        {
            base.Draw();
            DrawAccount();
        }

        public void DrawAccount()
        {
            EditorGUILayout.Separator();

            var labelWidth = Portal.PortalLoginDrawer.DrawOrganizationOptions();

            EditorGUILayout.Separator();

            Portal.PortalLoginDrawer.DrawProjectOptions(labelWidth);

            EditorGUILayout.Separator();
        }
    }

    public class TrackerIssue : StatusTracker
    {
        private readonly IIssueCondition Condition;
        private readonly IIssueSolution Solution;
        private double latestValidationTime;
        private bool lastConditionEvaluation = true;
        public GUIContent IssueWithTooltip;
        public GUIContent SolutionDescription;
        public GUIContent ButtonDescription;
        private Func<string> DocURL;
        public UpdateHandler[] UpdateHandlers;
        private bool AllowWordWrapInButton;

        public TrackerIssue(string identifier, string scopeId, Severity severity, IIssueCondition condition, IIssueSolution solution, GUIContent issueDescription, GUIContent solutionDescription, GUIContent buttonDescription, bool allowWordWrapInButton = true, Func<string> docURL = null, params UpdateHandler[] updateHandlers) : base(identifier, scopeId, severity, issueDescription)
        {
            Condition = condition;
            Solution = solution;
            SolutionDescription = solutionDescription;
            ButtonDescription = buttonDescription;
            UpdateHandlers = updateHandlers;
            DocURL = docURL;
            Message = new GUIContent(Message)
            {
                tooltip = solutionDescription.text
            };
            AllowWordWrapInButton = allowWordWrapInButton;
        }

        /// <summary>
        /// Dont run evaluation, but check if it passed the last time it was run
        /// </summary>
        /// <returns>The condition passed, there is no issue</returns>
        public bool GetLastEvaluation()
        {
            return lastConditionEvaluation;
        }

        /// <summary>
        /// Run the evaluation
        /// </summary>
        public void Evaluate()
        {
            latestValidationTime = EditorApplication.timeSinceStartup;
            lastConditionEvaluation = Condition.Evaluate();
        }

        public void Trigger()
        {
            Solution.Trigger();
            Evaluate();
        }

        internal void SetUpdateHandler(UpdateHandler[] handlers)
        {
            UpdateHandlers = handlers;
        }

        public string TimeSinceLastValidation()
        {
            var timespend = EditorApplication.timeSinceStartup - latestValidationTime;
            return TimeSpan.FromSeconds(timespend).ToString(@"mm\:ss");
        }

        public override bool Hidden => GetLastEvaluation();

        internal override void Init()
        {
            base.Init();

            foreach (var handler in UpdateHandlers)
            {
                switch (handler)
                {
                    case UpdateHandler.HierarchyChanged:
                        EditorApplication.hierarchyChanged += Evaluate;
                        break;
                    case UpdateHandler.ProjectChanged:
                        EditorApplication.projectChanged += Evaluate;
                        break;
                    case UpdateHandler.SelectionChanged:
                        Selection.selectionChanged += Evaluate;
                        break;
                    case UpdateHandler.OnAssetsChanged:
                        EditorApplication.projectChanged += Evaluate;
                        break;
                    case UpdateHandler.OnLoginStatusChange:
                        PortalLogin.OnLoggedIn += Evaluate;
                        PortalLogin.OnLoggedOut += Evaluate;
                        PortalLogin.OnProjectChanged += Evaluate;
                        break;
                    case UpdateHandler.OnBakeEnded:
                        BakeUtil.OnBakeEnded += () => Evaluate();
                        Schemas.OnSchemaStateUpdate += Evaluate;
                        break;
                    case UpdateHandler.Manual:
                    case UpdateHandler.OnEnable:
                        break;
                    default:
                        Debug.LogWarning("Did not handle Updatehandler " + handler.ToString());
                        break;
                }
            }

            Evaluate();
        }

        internal override void Draw()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                //Draw vertical layout
                if (Screen.width < 350)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        base.Draw();
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (CoherenceHubLayout.DrawButton(ButtonDescription, AllowWordWrapInButton))
                            {
                                Trigger();
                            }
                            if (!string.IsNullOrEmpty(DocURL?.Invoke()))
                            {
                                CoherenceHubLayout.DrawDocumentationLink(DocURL?.Invoke(), false);
                            }
                        }
                    }
                }
                //Draw horizonal layout
                else
                {
                    base.Draw();
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(buttonAreaWidth)))
                    {
                        if (CoherenceHubLayout.DrawButton(ButtonDescription, AllowWordWrapInButton))
                        {
                            Trigger();
                        }
                        if (!string.IsNullOrEmpty(DocURL?.Invoke()))
                        {
                            CoherenceHubLayout.DrawDocumentationLink(DocURL?.Invoke(), false);
                        }
                    }
                }
            }
        }
    }

    public struct IssueConditionCustom : IIssueCondition
    {
        public Func<bool> CheckValidity;

        public IssueConditionCustom(Func<bool> action)
        {
            CheckValidity = action;
        }

        public bool Evaluate()
        {
            if (CheckValidity != null)
            {
                return CheckValidity.Invoke();
            }
            else
            {
                return false;
            }
        }
    }

    public struct IssueSolutionAction : IIssueSolution
    {
        public Action OnTrigger;

        public IssueSolutionAction(Action action)
        {
            OnTrigger = action;
        }

        public void Trigger()
        {
            OnTrigger?.Invoke();
        }
    }

    public struct IssueSolutionDebug : IIssueSolution
    {
        public string DebugText;
        public LogLevel Verbosity;
        public Logger logger;

        public IssueSolutionDebug(string text, LogLevel logLevel, Logger moduleLog)
        {
            DebugText = text;
            Verbosity = logLevel;
            logger = moduleLog;
        }

        public void Trigger()
        {
            switch (Verbosity)
            {
                case LogLevel.Trace:
                    logger.Trace(DebugText);
                    break;
                case LogLevel.Debug:
                    logger.Debug(DebugText);
                    break;
                case LogLevel.Info:
                    logger.Info(DebugText);
                    break;
                case LogLevel.Warning:
                    logger.Warning(Warning.EditorHubTrackerIssueWarning, DebugText);
                    break;
                case LogLevel.Error:
                    logger.Error(Error.EditorHubTrackerIssueError, DebugText);
                    break;
            }
        }
    }

    public interface IIssueCondition
    {
        bool Evaluate();
    }

    public interface IIssueSolution
    {
        void Trigger();
    }
}
