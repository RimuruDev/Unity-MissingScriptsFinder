// ReSharper disable all

// **************************************************************** //
//
//   Copyright (c) RimuruDev. All rights reserved.
//   Contact:
//          - Gmail:    rimuru.dev@gmail.com
//          - GitHub:   https://github.com/RimuruDev
//          - LinkedIn: https://www.linkedin.com/in/rimuru/
//
// **************************************************************** //

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace AbyssMoth
{
    public sealed class MissingScriptsFinder : EditorWindow
    {
        [MenuItem("RimuruDev Tools/Find Missing Scripts")]
        public static void ShowWindow()
        {
            var window = GetWindow<MissingScriptsFinder>();
            window.titleContent = new GUIContent("Find Missing Scripts");
            window.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Find Missing Scripts in Scene"))
                FindMissingScriptsInScene();

            if (GUILayout.Button("Delete All Missing Scripts in Scene"))
                DeleteAllMissingScriptsInScene();

            if (GUILayout.Button("Find Missing Scripts in Prefabs"))
                FindMissingScriptsInPrefabs();

            if (GUILayout.Button("Delete All Missing Scripts in Prefabs"))
                DeleteAllMissingScriptsInPrefabs();
        }

        private static Transform[] FindSceneTransforms(bool includeInactive)
        {
#if UNITY_6000_0_OR_NEWER
            var inactiveMode = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            return FindObjectsByType<Transform>(inactiveMode, FindObjectsSortMode.None);
#elif UNITY_2020_1_OR_NEWER
            return FindObjectsOfType<Transform>(includeInactive);
#else
            if (!includeInactive)
                return FindObjectsOfType<Transform>();

            var all = Resources.FindObjectsOfTypeAll(typeof(Transform));
            var results = new List<Transform>(all.Length);

            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i] as Transform;
                if (t == null)
                    continue;

                var go = t.gameObject;
                if (go == null)
                    continue;

                if (!go.scene.IsValid())
                    continue;

                results.Add(t);
            }

            return results.ToArray();
#endif
        }

        private static void FindMissingScriptsInScene()
        {
            var transforms = FindSceneTransforms(true);
            var missingGameObjects = 0;
            var missingComponents = 0;

            foreach (var t in transforms)
            {
                if (t == null)
                    continue;

                var go = t.gameObject;
                if (go == null || !go.scene.IsValid())
                    continue;

                var missingInGo = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingInGo <= 0)
                    continue;

                missingGameObjects++;
                missingComponents += missingInGo;

                Debug.Log($"<color=yellow>Missing scripts: {missingInGo} in GameObject: {GetFullPath(go)}</color>", go);
            }

            if (missingComponents == 0)
            {
                Debug.Log("No missing scripts found in the scene.");
                return;
            }

            Debug.Log($"<color=magenta>Found {missingComponents} missing scripts on {missingGameObjects} GameObjects in the scene.</color>");
        }

        private static void DeleteAllMissingScriptsInScene()
        {
            var transforms = FindSceneTransforms(true);
            var removedCount = 0;

            foreach (var t in transforms)
            {
                if (t == null)
                    continue;

                var go = t.gameObject;
                if (go == null || !go.scene.IsValid())
                    continue;

                var missingInGo = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingInGo <= 0)
                    continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                removedCount += missingInGo;
            }

            if (removedCount == 0)
            {
                Debug.Log("No missing scripts found to delete in the scene.");
                return;
            }

            Debug.Log($"<color=magenta>Deleted {removedCount} missing scripts in the scene.</color>");
            EditorSceneManager.MarkAllScenesDirty();
        }

        private static void FindMissingScriptsInPrefabs()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var missingPrefabs = 0;
            var missingComponents = 0;

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                    continue;

                var components = prefab.GetComponentsInChildren<Component>(true);
                var missingInPrefab = 0;

                foreach (var component in components)
                {
                    if (component == null)
                        missingInPrefab++;
                }

                if (missingInPrefab <= 0)
                    continue;

                missingPrefabs++;
                missingComponents += missingInPrefab;

                Debug.Log($"<color=yellow>Missing scripts: {missingInPrefab} in Prefab: {path}</color>", prefab);
            }

            if (missingComponents == 0)
            {
                Debug.Log("No missing scripts found in any prefabs.");
                return;
            }

            Debug.Log($"<color=magenta>Found {missingComponents} missing scripts across {missingPrefabs} prefabs.</color>");
        }

        private static void DeleteAllMissingScriptsInPrefabs()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var removedPrefabs = 0;
            var removedComponents = 0;

            try
            {
                for (var i = 0; i < prefabGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                    EditorUtility.DisplayProgressBar(
                        "Missing Scripts",
                        $"Scanning prefabs... {i + 1}/{prefabGuids.Length}",
                        prefabGuids.Length <= 0 ? 1f : (float)(i + 1) / prefabGuids.Length
                    );

                    if (string.IsNullOrEmpty(path))
                        continue;

                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null && string.Equals(prefabStage.assetPath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning($"Prefab is currently open in Prefab Mode, skipped: {path}");
                        continue;
                    }

                    var root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null)
                        continue;

                    try
                    {
                        var removedInPrefab = 0;
                        var transforms = root.GetComponentsInChildren<Transform>(true);

                        foreach (var t in transforms)
                        {
                            if (t == null)
                                continue;

                            var go = t.gameObject;
                            if (go == null)
                                continue;

                            var missingInGo = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                            if (missingInGo <= 0)
                                continue;

                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                            removedInPrefab += missingInGo;
                        }

                        if (removedInPrefab <= 0)
                            continue;

                        PrefabUtility.SaveAsPrefabAsset(root, path, out var ok);
                        if (!ok)
                        {
                            Debug.LogError($"Failed to save prefab after cleanup: {path}");
                            continue;
                        }

                        removedPrefabs++;
                        removedComponents += removedInPrefab;

                        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        Debug.Log($"<color=magenta>Deleted {removedInPrefab} missing scripts in Prefab: {path}</color>", prefabAsset);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (removedComponents == 0)
            {
                Debug.Log("No missing scripts found to delete in any prefabs.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=magenta>Deleted {removedComponents} missing scripts across {removedPrefabs} prefabs.</color>");
        }

        private static string GetFullPath(GameObject go)
        {
            var path = "/" + go.name;

            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = "/" + go.name + path;
            }

            return path;
        }
    }
}
#endif