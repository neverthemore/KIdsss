// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Editor.Portal;
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 90)]
    public class OnlineModule : HubModule
    {
        public static class ModuleGUIContents
        {
            public static readonly GUIContent localDevelopmentMode = EditorGUIUtility.TrTextContent("Local Development Mode", "When enabled, the game will try to discover a local Replication Server. This also allows MultiRoomSimulatorLocalForwarder to perform HTTP requests to localhost.");
            public static readonly GUIContent localDevelopmentModeInfo = EditorGUIUtility.TrTextContent("Allows development features like localhost Replication Server discovery and Multi-Room Simulators' local forwarning. Disable this on release/distributable builds.");
            public static readonly GUIContent portal = EditorGUIUtility.TrTextContent("Dashboard", ExternalLinks.PortalUrl);
            public static readonly GUIContent login = EditorGUIUtility.TrTextContent("Login", "Login to coherence");
            public static readonly GUIContent logout = EditorGUIUtility.TrTextContent("Logout", "Logout");
            public static readonly GUIContent bakeFolderButton = EditorGUIUtility.IconContent("Project", "Select the bake folder in the Project window");
            public static readonly GUIContent syncStatus = EditorGUIUtility.TrTextContent("Status");
            public static readonly GUIContent sync = Icons.GetContent("Coherence.Sync", "Reload status state");
            public static readonly GUIContent upload = EditorGUIUtility.TrTextContent("Upload to coherence Cloud", "Upload active schemas to coherence Cloud");
            public static readonly GUIContent OnlineBenefits = new GUIContent("Make sure to login to the coherence dashboard to unlock the full potential of coherence. " +
                "\nCreating an online project will allow you to run Simulators in the cloud, upload builds and share your work with the rest of the world");
            public static readonly GUIContent RefreshSubscription = new GUIContent("Please refresh organizations above to see subscription info");
            public static readonly GUIContent FetchingSubscriptionData = new GUIContent("Fetching subscription data...");

            public static readonly GUIContent Account = new GUIContent("Account");
            public static readonly GUIContent Schemas = new GUIContent("Schemas");
            public static readonly GUIContent ShareBuild = new GUIContent("Share build");
            public static readonly GUIContent SelectOrgAndProject = new GUIContent("You are logged in but need to select an organization and a project.");
        }

        public override string ModuleName => "coherence Cloud";

        public override HelpSection Help => new HelpSection()
        {
            title = new GUIContent("Getting the most out of coherence"),
            content = ModuleGUIContents.OnlineBenefits
        };

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();

            if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.OrganizationID))
            {
                PortalLoginDrawer.RefreshSubscriptionInfo();
            }

            GameBuildWindow.RestoreSavedBuildSettings();
        }

        public override void OnModuleDisable()
        {
            base.OnModuleDisable();
        }

        public override void OnBespokeGUI()
        {
            base.OnBespokeGUI();
            CoherenceHubLayout.DrawSection(ModuleGUIContents.Account, DrawAccount, DrawDashboardLink);
            if (!PortalUtil.OrgAndProjectIsSet)
            {
                EditorGUILayout.HelpBox("You must be logged in and have an organization and project selected before you can upload schemas and share builds.", MessageType.Warning);
                GUI.enabled = false;
            }
            CoherenceHubLayout.DrawSection(ModuleGUIContents.Schemas, SharedModuleSections.DrawSchemasInPortal);
            CoherenceHubLayout.DrawSection(ModuleGUIContents.ShareBuild, GameBuildWindow.DrawShareBuildGUI);
            GUI.enabled = true;
        }

        public void DrawDashboardLink()
        {
            var url = SharedModuleSections.GetDashboardUrl(PortalLoginDrawer.GetSelectedOrganization()?.slug);
            var content = ModuleGUIContents.portal;
            content.tooltip = url;
            CoherenceHubLayout.DrawLink(content, url);
        }

        public void DrawAccount()
        {
            bool loginToken = !string.IsNullOrEmpty(ProjectSettings.instance.LoginToken);

            if (!loginToken)
            {
                DrawLoggedOutOptions();
            }
            else
            {
                if (PortalLoginDrawer.GetSelectedOrganization() == null)
                {
                    CoherenceHubLayout.DrawMessageArea(ModuleGUIContents.SelectOrgAndProject);
                }

                EditorGUILayout.Separator();
                using (new EditorGUILayout.HorizontalScope())
                {
                    CoherenceHubLayout.DrawBoldLabel(new GUIContent(ProjectSettings.instance.Email), null, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (CoherenceHubLayout.DrawButton(ModuleGUIContents.logout))
                    {
                        PortalLogin.Logout();
                    }
                }
                EditorGUILayout.Separator();
            }

            EditorGUILayout.Separator();

            var hasOrgID = !string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.OrganizationID);

            var labelWidth = PortalLoginDrawer.DrawOrganizationOptions();

            EditorGUILayout.Separator();

            PortalLoginDrawer.DrawProjectOptions(labelWidth);

            EditorGUILayout.Separator();

            if (loginToken && hasOrgID)
            {
                var r = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, .3f));

                EditorGUILayout.Separator();

                DrawSubscription();

                EditorGUILayout.Separator();
            }
        }

        public void DrawSubscription()
        {
            var subscription = PortalLoginDrawer.OrgSubscription;
            if (PortalLoginDrawer.GetSubscriptionDataRequest != null)
            {
                CoherenceHubLayout.DrawLabel(ModuleGUIContents.FetchingSubscriptionData);
            }
            else if (subscription != null)
            {
                CoherenceHubLayout.DrawLabel(new GUIContent($"Plan: {subscription.product_name}"));

                var creditsLeft = subscription.credits_included - subscription.credits_consumed;
                var percentageUsed = 1f - creditsLeft / (float)subscription.credits_included;
                var billingUrl = SharedModuleSections.GetOrganizationBillingUrl(PortalLoginDrawer.GetSelectedOrganization()?.name);

                CoherenceHubLayout.DrawLabel(new GUIContent($"Credits: {Mathf.Max(creditsLeft, 0)} / {subscription.credits_included}"));
                if (subscription.credits_consumed == 0)
                {
                }
                else if (creditsLeft > 0)
                {
                    var iconName = percentageUsed >= 0.75f ? "Warning" : "Info";
                    CoherenceHubLayout.DrawBoldLabel(EditorGUIUtility.TrTextContentWithIcon($"You've used {Mathf.CeilToInt(percentageUsed * 100)}% of your monthly credits.", iconName));

                    EditorGUILayout.Space();

                    if (CoherenceHubLayout.DrawButtonBlue(EditorGUIUtility.TrTextContent("Manage Cloud plan")))
                    {
                        Application.OpenURL(billingUrl);
                    }
                }
                else if (creditsLeft <= 0)
                {
                    CoherenceHubLayout.DrawBoldLabel(EditorGUIUtility.TrTextContentWithIcon("You've used all your monthly credits.", "Error"));

                    if (creditsLeft < 0)
                    {
                        if (subscription.product_name != "Free")
                        {
                            CoherenceHubLayout.DrawBoldLabel(new GUIContent($"Credit Overage: {Mathf.Abs(creditsLeft)}"));
                            CoherenceHubLayout.DrawBoldLabel(EditorGUIUtility.TrTextContent("Overage will be charged on the monthly bill."));
                        }
                    }

                    EditorGUILayout.Space();

                    if (CoherenceHubLayout.DrawButtonBlue(EditorGUIUtility.TrTextContent("Upgrade Cloud plan")))
                    {
                        Application.OpenURL(billingUrl);
                    }
                }
            }
            else
            {
                CoherenceHubLayout.DrawLabel(ModuleGUIContents.RefreshSubscription);
            }
        }

        private void DrawLoggedOutOptions()
        {
            CoherenceHubLayout.DrawInfoAreaWithBulletPoints(
                "You are not logged in. Please log in, select an organization and project to:",
                "Upload schemas or builds.", "Access your project tokens.", "Access your coherence dashboard");

            EditorGUILayout.Separator();

            if (CoherenceHubLayout.DrawButtonBlue("Signup / Login"))
            {
                Repaint();
                PortalLogin.Login(Repaint);
            }
        }
    }
}
