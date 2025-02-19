// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using Logger = Log.Logger;

    public interface IModuleWindow : IAnyHubModule
    {
        HubModule GetData();
    }

    public interface IAnyHubModule
    {
        public Logger Logger { get; set; }
    }

    public abstract class ModuleWindow<TWindow, TData> : BaseModuleWindow, IModuleWindow where TWindow : EditorWindow, IModuleWindow where TData : HubModule
    {
        public Logger Logger { get; set; }

        public string WindowName => typeof(TData).Name;

        public static void OpenWindow(bool shouldOpen)
        {
            if (shouldOpen)
            {
                _ = GetWindow<TWindow>();
            }
            else
            {
                GetWindow<TWindow>().Close();
            }
        }

        public HubModule GetData()
        {
            return HubModuleManager.instance.GetActiveModule(this);
        }

        protected override void Awake()
        {
            base.Awake();
            HubModuleManager.instance.AssignModule(this, out TData m, HubModuleManager.AssignStrategy.ForceReassign);
            module = m;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return base.Equals(other);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void SaveChanges()
        {
            base.SaveChanges();
        }

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            return base.GetExtraPaneTypes();
        }
    }
}
