#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.

public static class MissingScriptTools
{
    #region Scene

    [MenuItem("Tools/Missing Scripts/Find In Open Scenes")]
    public static void FindInOpenScenes()
    {
        var found = new List<GameObject>();
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missing > 0)
            {
                found.Add(go);
                Debug.Log($"[Missing] {GetHierarchyPath(go)}  (missing:{missing})  | Other Components: {ListOtherComponents(go)}", go);
            }
        }

        if (found.Count == 0)
        {
            Debug.Log("[MissingScriptTools] No missing scripts in open scenes");
            return;
        }

        Selection.objects = found.ToArray();
        Debug.Log($"[MissingScriptTools] Found {found.Count} objects with missing scripts (selected in Hierarchy)");
    }

    [MenuItem("Tools/Missing Scripts/Remove In Open Scenes")]
    public static void RemoveInOpenScenes()
    {
        if (!EditorUtility.DisplayDialog("Remove Missing Scripts",
                "열려 있는 모든 씬의 Missing(MonoBehaviour) 컴포넌트를 제거할까요?\n이 작업은 되돌리기(Undo) 가능하지만 커밋 전 백업을 권장합니다.",
                "제거", "취소")) return;

        int removedTotal = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
            removedTotal += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        Debug.Log($"[MissingScriptTools] Removed {removedTotal} missing components from open scenes");
        EditorSceneManager.MarkAllScenesDirty();
    }

    #endregion

    #region Prefab

    [MenuItem("Tools/Missing Scripts/Find In All Prefabs")]
    public static void FindInAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        var found = new List<Object>();
        int totalMissing = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (i % 200 == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Find Missing Scripts in Prefabs",
                        $"{i}/{guids.Length}  {path}",
                        (float)i / guids.Length))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.Log($"[MissingScriptTools] Cancelled at {i}/{guids.Length}");
                    return;
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int missing = CountMissingInHierarchy(prefab);
            if (missing > 0)
            {
                totalMissing += missing;
                found.Add(prefab);
                Debug.Log($"[Missing Prefab] {path}  (missing:{missing})", prefab);
            }
        }

        EditorUtility.ClearProgressBar();

        if (found.Count == 0)
        {
            Debug.Log("[MissingScriptTools] No missing scripts in prefabs");
            return;
        }

        Selection.objects = found.ToArray();
        Debug.Log($"[MissingScriptTools] Found {totalMissing} missing components in {found.Count} prefabs (selected in Project)");
    }

    [MenuItem("Tools/Missing Scripts/Remove In All Prefabs")]
    public static void RemoveInAllPrefabs()
    {
        if (!EditorUtility.DisplayDialog("Remove Missing Scripts in Prefabs",
                "프로젝트 내 모든 프리팹의 Missing(MonoBehaviour) 컴포넌트를 제거할까요?\n프리팹 에셋이 직접 수정되므로 커밋 전 백업을 권장합니다.",
                "제거", "취소")) return;

        var guids = AssetDatabase.FindAssets("t:Prefab");
        int removedTotal = 0;
        int modifiedPrefabs = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (i % 200 == 0)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Remove Missing Scripts in Prefabs",
                            $"{i}/{guids.Length}  {path}",
                            (float)i / guids.Length))
                    {
                        break;
                    }
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (CountMissingInHierarchy(prefab) == 0) continue;

                // 프리팹 편집 모드로 열어서 수정
                var root = PrefabUtility.LoadPrefabContents(path);
                int removed = RemoveMissingInHierarchy(root);
                if (removed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    removedTotal += removed;
                    modifiedPrefabs++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[MissingScriptTools] Removed {removedTotal} missing components from {modifiedPrefabs} prefabs");
    }

    // 하위 오브젝트 포함 Missing Script 수 카운트
    private static int CountMissingInHierarchy(GameObject root)
    {
        int count = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        }
        return count;
    }

    // 하위 오브젝트 포함 Missing Script 제거
    private static int RemoveMissingInHierarchy(GameObject root)
    {
        int removed = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        return removed;
    }

    #endregion

    #region Utility

    // 하이어라키 경로 문자열 생성
    private static string GetHierarchyPath(GameObject go)
    {
        var path = go.name;
        var t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }

    // Missing이 아닌 컴포넌트 이름 목록
    private static string ListOtherComponents(GameObject go)
    {
        var comps = go.GetComponents<Component>();
        var names = new List<string>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            names.Add(c.GetType().Name);
        }
        return string.Join(", ", names);
    }

    #endregion
}
#endif