// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif


namespace Coherence.Cloud
{
#if UNITY
    using UnityEngine;
#endif
    using Newtonsoft.Json;
    using Prefs;
    using Runtime;
    using System;
    using System.Collections.Generic;

    internal class UniqueIdPool
    {
        private static Dictionary<string, UniqueIdPool> idPoolsForProject =
            new Dictionary<string, UniqueIdPool>();

        private List<string> allIdsPool = new List<string>();
        private Stack<string> inUseIdsPool = new Stack<string>();

#if UNITY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPoolInUnity()
        {
            idPoolsForProject.Clear();
        }
#endif

        public static string Get(string projectId)
        {
            UniqueIdPool idPool = GetIdPoolForProject(projectId);

            if (idPool.inUseIdsPool.Count <= 0)
            {
                return GenerateNewId(projectId, idPool);
            }

            var uniqueId = idPool.inUseIdsPool.Pop();

            return uniqueId;
        }

        internal static bool TryGet(string projectId, out string uniqueId)
        {
            UniqueIdPool idPool = GetIdPoolForProject(projectId);

            if (idPool.inUseIdsPool.Count <= 0)
            {
                uniqueId = null;
                return false;
            }

            uniqueId = idPool.inUseIdsPool.Pop();
            return true;
        }

        public static void Release(string projectId, string idToRelease)
        {
            UniqueIdPool guestAccountPool = GetIdPoolForProject(projectId);

            guestAccountPool.inUseIdsPool.Push(idToRelease);
        }

        private static UniqueIdPool GetIdPoolForProject(string projectId)
        {
            if (idPoolsForProject.TryGetValue(projectId, out UniqueIdPool idPool))
            {
                return idPool;
            }

            idPool = new UniqueIdPool();
            idPoolsForProject[projectId] = idPool;

            InitializeIdPool(projectId, idPool);

            return idPool;
        }

        private static void InitializeIdPool(string projectId, UniqueIdPool idPool)
        {
            var key = GetKeyForProject(projectId);
            var json = Prefs.GetString(key);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            idPool.allIdsPool = Utils.CoherenceJson.DeserializeObject<List<string>>(json) ?? new List<string>();

            idPool.inUseIdsPool = new Stack<string>();

            for (int i = idPool.allIdsPool.Count - 1; i >= 0; i--)
            {
                idPool.inUseIdsPool.Push(idPool.allIdsPool[i]);
            }
        }

        private static string GenerateNewId(string projectId, UniqueIdPool idPool)
        {
            var newId = Guid.NewGuid().ToString();

            idPool.allIdsPool.Add(newId);

            Prefs.SetString(GetKeyForProject(projectId), Utils.CoherenceJson.SerializeObject(idPool.allIdsPool));
            return newId;
        }

        private static string GetKeyForProject(string projectId)
        {
            return Runtime.Utils.PrefsUtils.Format(PrefsKeys.UniqueIdPool, projectId);
        }
    }
}
