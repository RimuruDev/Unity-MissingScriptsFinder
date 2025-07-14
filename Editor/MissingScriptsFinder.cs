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

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace AbyssMoth
{
    public sealed class MissingScriptsFinder : EditorWindow
    {
        [MenuItem("RimuruDev Tools/Find Missing Scripts")]
        public static void ShowWindow() =>
            GetWindow<MissingScriptsFinder>("Find Missing Scripts");

        private void OnGUI()
        {
            if (GUILayout.Button("Find Missing Scripts in Scene"))
                FindMissingScriptsInScene();

            if (GUILayout.Button("Delete All Missing Scripts in Scene"))
                DeleteAllMissingScriptsInScene();

            if (GUILayout.Button("Find Missing Scripts in Prefabs"))
                FindMissingScriptsInPrefabs();
        }

        private static GameObject[] FindGameObjects(bool includeInactive = true)
        {
#if UNITY_6000_0_OR_NEWER
            var inactiveMode = includeInactive
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;

            return Object.FindObjectsByType<GameObject>(inactiveMode, FindObjectsSortMode.InstanceID);
#else
            return Object.FindObjectsOfType<GameObject>(includeInactive);
#endif
        }


        private static void FindMissingScriptsInScene()
        {
            var objects = FindGameObjects();
            var missingCount = 0;

            foreach (var go in objects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        missingCount++;
                        Debug.Log($"<color=yellow>Missing script found in GameObject: {GetFullPath(go)}</color>", go);
                    }
                }
            }

            Debug.Log(missingCount == 0
                ? "No missing scripts found in the scene."
                : $"<color=magenta>Found {missingCount} GameObjects with missing scripts in the scene.</color>");
        }

        private static void DeleteAllMissingScriptsInScene()
        {
            var objects = FindGameObjects();
            var removedCount = 0;

            foreach (var go in objects)
            {
                var components = go.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        removedCount++;
                    }
                }
            }

            Debug.Log(removedCount == 0
                ? "No missing scripts found to delete in the scene."
                : $"<color=magenta>Deleted {removedCount} missing scripts in the scene.</color>");

            EditorSceneManager.MarkAllScenesDirty();
        }

        private static void FindMissingScriptsInPrefabs()
        {
            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            var missingCount = 0;

            foreach (var guid in prefabGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var components = prefab.GetComponentsInChildren<Component>(true);

                foreach (var component in components)
                {
                    if (component == null)
                    {
                        missingCount++;
                        Debug.Log($"<color=yellow>Missing script found in Prefab: {path}</color>", prefab);
                    }
                }
            }

            Debug.Log(missingCount == 0
                ? "No missing scripts found in any prefabs."
                : $"<color=magenta>Found {missingCount} prefabs with missing scripts.</color>");
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
