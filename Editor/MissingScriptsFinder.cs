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
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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

                var components = go.GetComponents<Component>();
                var missingInGo = 0;

                foreach (var component in components)
                {
                    if (component == null)
                        missingInGo++;
                }

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

                var components = go.GetComponents<Component>();
                var missingInGo = 0;

                foreach (var component in components)
                {
                    if (component == null)
                        missingInGo++;
                }

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