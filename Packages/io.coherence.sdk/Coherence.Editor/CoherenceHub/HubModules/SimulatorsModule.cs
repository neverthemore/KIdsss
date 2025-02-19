// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Build;
    using Connection;
    using System;
    using UnityEditor;
    using UnityEngine;

    [Serializable, HubModule(Priority = 40)]
    public class SimulatorsModule : HubModule
    {
        private SimulatorBuildOptions buildOptions;

        [SerializeField]
        private EndpointData endpoint;

        public override string ModuleName => "Simulators";

        public override HelpSection Help => new HelpSection()
        {
            title = new GUIContent("Telling Simulators apart from Clients"),
            content = ModuleGUIContents.WhatAreSims
        };

        protected class ModuleGUIContents
        {
            public static readonly GUIContent WhatAreSims = EditorGUIUtility.TrTextContent("When compiling your project, use the correct preprocessor definition (or command line parameter for local testing) to let coherence know that this Client build is a Simulator, not a Client. Check out documentation for more information and examples");
        }

        public override void OnBespokeGUI()
        {
            Init();

            CoherenceHubLayout.DrawSection("Simulator Build Options", DrawBuildOptions);
            CoherenceHubLayout.DrawSection("Headless Linux Build", DrawCreateSimulatorBuild);
            CoherenceHubLayout.DrawSection("AutoSimulatorConnection Component", DrawAutoSimulatorConnection);
            CoherenceHubLayout.DrawSection("Local Simulator Build", DrawLocalSimulatorBuild);
            CoherenceHubLayout.DrawSection("Run Local Simulator Build", DrawRunLocalSimulatorBuild);
        }

        public override void OnModuleEnable()
        {
            base.OnModuleEnable();
            endpoint = SimulatorEditorUtility.LastUsedEndpoint;
            Init();
        }

        private void Init()
        {
            if (buildOptions == null)
            {
                buildOptions = SimulatorBuildOptions.Get();
            }
        }

        private void DrawBuildOptions()
        {
            SimulatorGUI.DrawSimulatorBuildOptions(buildOptions, Host.position.width);
        }

        private void DrawCreateSimulatorBuild()
        {
            SimulatorGUI.DrawCreateAndUploadHeadlessSimulatorBuild(buildOptions);
        }

        private void DrawLocalSimulatorBuild()
        {
            SimulatorGUI.DrawLocalSimulatorBuild(buildOptions);
        }

        private void DrawAutoSimulatorConnection()
        {
            SimulatorGUI.DrawAutoSimulatorConnection();
        }

        private void DrawRunLocalSimulatorBuild()
        {
            SimulatorGUI.DrawRunSimulatorSettings(ref endpoint);
        }
    }
}
