// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
namespace Coherence.Prefs
{
    using UnityEngine;

    public class UnityPrefs : IPrefsImplementation
    {
        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public float GetFloat(string key)
        {
            return PlayerPrefs.GetFloat(key);
        }

        public float GetFloat(string key, float defaultValue)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public int GetInt(string key)
        {
            return PlayerPrefs.GetInt(key);
        }

        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }
    }
}

#endif // UNITY_5_3_OR_NEWER
