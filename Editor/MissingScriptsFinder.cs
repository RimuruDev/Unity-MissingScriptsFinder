// ReSharper disable all

// **************************************************************** //
//
//   Copyright (c) RimuruDev. All rights reserved.
//   Contact me: 
//          - Gmail:    rimuru.dev@gmail.com
//          - GitHub:   https://github.com/RimuruDev
//          - LinkedIn: https://www.linkedin.com/in/rimuru/
//
// **************************************************************** //

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Runtime.Remoting.Activation;

namespace AbyssMoth
{
    [Url("https://github.com/RimuruDev/Unity-MissingScriptsFinder")]
    public sealed class MissingScriptsFinder : EditorWindow
    {
        [MenuItem("RimuruDev Tools/Find Missing Scripts")]
        public static void ShowWindow() =>
            GetWindow<MissingScriptsFinder>("Find Missing Scripts");

        private void OnGUI()
        {
            if (GUILayout.Button("Find Missing Scripts in Scene"))
            {
                FindMissingScripts();
            }

            if (GUILayout.Button("Delete All Missing Scripts"))
            {
                DeleteAllMissingScripts();
            }
        }

        private static void FindMissingScripts()
        {
            var objects = FindObjectsOfType<GameObject>(true);
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
                : $"<color=magenta>Found {missingCount} GameObjects with missing scripts.</color>");
        }

        private static void DeleteAllMissingScripts()
        {
            var objects = GameObject.FindObjectsOfType<GameObject>(true);
            var removedCount = 0;

            foreach (var go in objects)
            {
                var components = go.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null)
                    {
                        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        removedCount++;
                    }
                }
            }

            Debug.Log(removedCount == 0
                ? "No missing scripts found to delete."
                : $"<color=magenta>Deleted {removedCount} missing scripts.</color>");

            EditorSceneManager.MarkAllScenesDirty();
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