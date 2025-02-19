// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 100)]
    public class OverviewModule : HubModule
    {
        private struct ValidatedSections
        {
            public GUIContent Title;
            public Func<bool> Validator;
            public Action IssueDrawer;
            public Action SuccessDrawer;

            public ValidatedSections(GUIContent title, Func<bool> validator, Action issueDrawer, Action successDrawer = null) : this()
            {
                Title = title;
                Validator = validator;
                IssueDrawer = issueDrawer;
                SuccessDrawer = successDrawer;
            }
        }

        public class ModuleGUIContents
        {
            public static readonly GUIContent VersioningSection = EditorGUIUtility.TrTextContent("Version");
            public static readonly GUIContent VersionUpToDate = EditorGUIUtility.TrTextContent($"coherence is up to date. Installed version is {CoherenceHub.info.SDKVersionString}");
            public static readonly GUIContent VersionUpdateAvaliable = EditorGUIUtility.TrTextContent("There is a new version of coherence avaliable for download.");
            public static readonly GUIContent WelcomeToHub = new GUIContent("Welcome to coherence Hub");
            public static readonly GUIContent WelcomeContent = new GUIContent("The Hub is here to help you use coherence more efficiently. You can check the general status of coherence, and ensure your setup and versions are up to date.");
            public static readonly GUIContent ProjectStatusGood = new GUIContent("Check back here for updates and warnings or refer to our Links Tab for more help.");

            public static readonly GUIContent AccountStatusHasIssues = new GUIContent("Check your access to coherence hosting and publishing features.", "To access game hosting and publishing features you should make sure your account, project, and organization are all set up.");
            public static readonly GUIContent ProjectStatusHasIssues = new GUIContent("Check the status of your coherence setup, diagnose and fix issues.", "Check the status of coherence to make sure your setup is correct. Fix and diagnose warnings, and ensure you don�t have any active roadblocks.");
        }

        private List<ValidatedSections> validatedSections;

        public override string ModuleName => "Overview";

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();
        }

        private void InitSections()
        {
            validatedSections = new List<ValidatedSections>()
            {
                new ValidatedSections(
                    new GUIContent("Account Status"),
                    () => false,
                    () => DrawAccountStatusIssues(),
                    () => DrawSubscription()
                    ),
                new ValidatedSections(
                    new GUIContent("Project Status and Setup"),
                    () => false,
                    () => DrawProjectStatusIssues(),
                    () => DrawProjectStatusGood()
                    )
            };
        }

        internal void DrawAccountStatusIssues()
        {
            DrawSubscription();
        }

        internal void DrawProjectStatusIssues()
        {
            CoherenceHubLayout.DrawLabel(ModuleGUIContents.ProjectStatusHasIssues);
            CoherenceHubLayout.DrawIssuesListForScope(StatusTrackerConstructor.Scopes.ProjectStatus);
        }

        private void DrawProjectStatusGood()
        {
            CoherenceHubLayout.DrawLabel(ModuleGUIContents.ProjectStatusGood);
        }

        public void DrawSubscription()
        {
            if (string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
                return;

            var subscription = Portal.PortalLoginDrawer.OrgSubscription;
            if (Portal.PortalLoginDrawer.GetSubscriptionDataRequest != null)
            {
                CoherenceHubLayout.DrawLabel(OnlineModule.ModuleGUIContents.FetchingSubscriptionData);
            }
            else if (subscription != null)
            {
                var creditsLeft = subscription.credits_included - subscription.credits_consumed;

                if (creditsLeft == 0)
                {
                    CoherenceHubLayout.DrawLabel(new GUIContent("Credits Remaining: 0"));
                    CoherenceHubLayout.DrawBoldLabel(EditorGUIUtility.TrTextContentWithIcon("Out of credits. Check the coherence Cloud tab for more information.", "Warning"));
                }
                else if (creditsLeft < 0)
                {
                    CoherenceHubLayout.DrawLabel(new GUIContent($"Credits Overage: {Mathf.Abs(creditsLeft)}"));
                    CoherenceHubLayout.DrawBoldLabel(EditorGUIUtility.TrTextContentWithIcon("Your credits are in overage. Check the coherence Cloud tab for more information.", "Warning"));
                }
                else
                {
                    CoherenceHubLayout.DrawLabel(new GUIContent($"Credits Remaining: {creditsLeft} / {subscription.credits_included}"));
                }
            }
            else
            {
                CoherenceHubLayout.DrawLabel(OnlineModule.ModuleGUIContents.RefreshSubscription);
            }
        }

        public override void OnBespokeGUI()
        {
            base.OnBespokeGUI();

            if (validatedSections == null)
            {
                InitSections();
            }

            CoherenceHubLayout.DrawDismissableSection(this.ModuleName, ModuleGUIContents.WelcomeToHub, ModuleGUIContents.WelcomeContent);

            foreach (var section in validatedSections)
            {
                CoherenceHubLayout.DrawValidatedSection(section.Title, section.Validator, section.IssueDrawer, section.SuccessDrawer);
            }
        }
    }
}
