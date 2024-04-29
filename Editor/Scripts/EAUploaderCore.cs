﻿using EAUploader.CustomPrefabUtility;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EAUploader
{
    [InitializeOnLoad]
    public class EAUploaderCore
    {
        [Serializable]
        private class PackageJson
        {
            public string version;
        }

        public static event Action<string> SelectedPrefabPathChanged;

        private static string _selectedPrefabPath = null;
        public static string selectedPrefabPath
        {
            get => _selectedPrefabPath;
            set
            {
                if (_selectedPrefabPath != value)
                {
                    _selectedPrefabPath = value;
                    SelectedPrefabPathChanged?.Invoke(value);
                }
            }
        }

        private const string EAUPLOADER_ASSET_PATH = "Assets/EAUploader";
        private static bool initializationPerformed = false;
        public static bool HasVRM = false;

        static EAUploaderCore()
        {
            Debug.Log("EAUploader is starting...");
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && !initializationPerformed)
            {
                EditorApplication.update -= OnEditorUpdate; // Unregister after initialization
                initializationPerformed = true;
                PerformInitialization();
            }
        }

        private static void PerformInitialization()
        {
            try
            {
                InitializeEAUploader();
                EAUploaderEditorManager.OnEditorManagerLoad();
                ShaderChecker.CheckShaders();
                PrefabManager.Initialize();
                CheckIsVRMAvailable();

                // [EAUPlugin]
                var methods = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    .Where(m => m.GetCustomAttributes(typeof(EAUPluginAttribute), false).Length > 0)
                    .ToList();

                foreach (var method in methods)
                {
                    method.Invoke(null, null);
                }

                // Wait for the above processes to complete before opening the EAUploader window
                EditorApplication.delayCall += OpenEAUploaderWindow;

                Application.logMessageReceived += UI.Windows.Logger.OnReceiveLog;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                if (EditorUtility.DisplayDialog("EAUploader Error", T7e.Get("Restart EAUploader because there is a problem with EAUploader; if restarting EAUploader does not solve the problem, delete EAUploader from VCC and add EAUploader again."), T7e.Get("Restart"), T7e.Get("Cancel")))
                {
                    AssetDatabase.ImportAsset("Packages/tech.uslog.eauploader", ImportAssetOptions.ImportRecursive);
                }
            }
        }

        private static void CheckIsVRMAvailable()
        {
            try
            {
                string manifestPath = "Packages/packages-lock.json";
                if (File.Exists(manifestPath))
                {
                    string manifestContent = File.ReadAllText(manifestPath);
                    HasVRM = manifestContent.Contains("\"com.vrmc.univrm\"") && manifestContent.Contains("\"jp.pokemori.vrm-converter-for-vrchat\"");
                }
                else
                {
                    Debug.LogError("Manifest file not found.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to check for packages: " + e.Message);
            }
        }

        private static void InitializeEAUploader()
        {
            // Create New folder
            if (!AssetDatabase.IsValidFolder(EAUPLOADER_ASSET_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "EAUploader");
            }

            string prefabManagerPath = $"{EAUPLOADER_ASSET_PATH}/PrefabManager.json";
            if (!File.Exists(prefabManagerPath))
            {
                File.WriteAllText(prefabManagerPath, "{}");
            }
        }

        private static void OpenEAUploaderWindow()
        {
            // 既存のウィンドウを検索
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(window => window.GetType().Name == "EAUploader").ToList();

            Debug.Log($"EAUploader windows found: {windows.Count}");

            if (windows.Count == 0)
            {
                Debug.Log("Attempting to open EAUploader...");
                bool result = EditorApplication.ExecuteMenuItem("EAUploader/Open EAUploader");
                Debug.Log($"EAUploader opened: {result}");
            }
            else
            {
                Debug.Log("Focusing on existing EAUploader window.");
                windows[0].Focus();
            }
        }

        public static string GetVersion(bool noText = false)
        {
            // Get version from package.json
            string packageJsonPath = "Packages/tech.uslog.eauploader/package.json";
            if (File.Exists(packageJsonPath))
            {
                string packageJson = File.ReadAllText(packageJsonPath);
                if (noText)
                {
                    return JsonUtility.FromJson<PackageJson>(packageJson).version;
                }
                else
                {
                    return T7e.Get("Version: ") + JsonUtility.FromJson<PackageJson>(packageJson).version;
                }
            }

            if (noText)
            {
                return "Unknown";
            }
            else
            {
                return T7e.Get("Version: ") + "Unknown";
            }
        }

        [MenuItem("EAUploader/Reload")]
        public static void ReloadSDK()
        {
            selectedPrefabPath = null;
            EAUploaderEditorManager.OnEditorManagerLoad();
            ShaderChecker.CheckShadersInPrefabs();
            PrefabManager.Initialize();
            CheckIsVRMAvailable();
        }
    }
}