﻿using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace EAUploader
{
    [InitializeOnLoad]
    internal class EAUploaderCore
    {
        private const string EAUPLOADER_ASSET_PATH = "Assets/EAUploader";
        private static bool initializationPerformed = false;
        public static string selectedPrefabPath = null;
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
            InitializeEAUploader();
            EAUploaderEditorManager.OnEditorManagerLoad();
            ShaderChecker.CheckShadersInPrefabs();
            CustomPrefabUtility.PrefabManager.Initialize();
            CheckIsVRMAvailable();
            OpenEAUploaderWindow();
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

            string tailwindUssPath = $"{EAUPLOADER_ASSET_PATH}/UI/tailwind.uss";
            if (!File.Exists(tailwindUssPath))
            {
                string tailwindUss = Resources.Load<TextAsset>("UI/tailwind").text;
                File.WriteAllText(tailwindUssPath, tailwindUss);
            }

            string notoSansJPPath = $"{EAUPLOADER_ASSET_PATH}/UI/Noto_Sans_JP.ttf";
            if (!File.Exists(notoSansJPPath))
            {
                TextAsset notoSansJPAsset = Resources.Load("UI/Noto_Sans_JP") as TextAsset;
                byte[] notoSansJP = notoSansJPAsset.bytes;
                File.WriteAllBytes(notoSansJPPath, notoSansJP);
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
    }
}
