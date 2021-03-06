﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_EDITOR

public class AutomaticChecker
{
    const string DefaultRootPath = "Assets";
    const string SceneExtension = ".unity";
    const string PrefabExtension = ".prefab";

    [MenuItem("Check/All")]
    static void CheckAll()
    {
        Check(EnumerateFilePaths(DefaultRootPath));
    }

    [MenuItem("Check/Selecting")]
    static void CheckSelecting()
    {
        Check(GetSelectingFilePaths());
    }

    static void Check(IEnumerable<string> filePaths)
    {
        List<string> prefabPaths = new List<string>();
        List<string> scenePaths = new List<string>();

        foreach (var path in filePaths)
        {
            string extension = Path.GetExtension(path);

            switch (extension)
            {
                case PrefabExtension:
                    prefabPaths.Add(path);
                    break;
                case SceneExtension:
                    scenePaths.Add(path);
                    break;
            }
        }

        for (int i = 0; i < prefabPaths.Count; i++)
        {
            string path = prefabPaths[i];
            EditorUtility.DisplayProgressBar("Checking prefabs", $"({i}/{prefabPaths.Count}) {path}", (float)i / (float)prefabPaths.Count);
            CheckPrefab(path);
        }

        if (scenePaths.Any())
            CheckScenes(scenePaths);

        EditorUtility.DisplayProgressBar("Saving assets", "Final phase", 0f);

        AssetDatabase.SaveAssets();

        EditorUtility.ClearProgressBar();
    }

    static void CheckScenes(List<string> scenePaths)
    {
        int openingSceneCount = EditorSceneManager.sceneCount;

        // 現在編集中のシーンを保存する
        EditorSceneManager.SaveOpenScenes();

        // 処理後にシーン状態を戻すために現在のシーン一覧を記録しておく
        string[] openingScenePaths = Enumerable.Range(0, openingSceneCount)
            .Select(i => EditorSceneManager.GetSceneAt(i))
            // 保存されていないシーンは取り除く
            .Where(s => !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        // 既に開かれているシーンを再度開こうとすると処理が止まるので、新規シーンのみにする
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        for (int i = 0; i < scenePaths.Count; i++)
        {
            string path = scenePaths[i];
            EditorUtility.DisplayProgressBar("Checking scenes", $"({i}/{scenePaths.Count}) {path}", (float)i / (float)scenePaths.Count);
            CheckScene(path);
        }

        // 編集前のシーン状態に戻す
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (openingScenePaths.Any())
        {
            EditorSceneManager.OpenScene(openingScenePaths[0], OpenSceneMode.Single);
            for (int i = 1; i < openingScenePaths.Length; i++)
                EditorSceneManager.OpenScene(openingScenePaths[i], OpenSceneMode.Additive);
        }
    }

    static void CheckScene(string path)
    {
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        bool hasChanged = false;

        foreach (var obj in GameObject.FindObjectsOfType<GameObject>())
        {
            GameObject original = PrefabUtility.GetPrefabParent(obj) as GameObject;
            GameObject root = PrefabUtility.FindPrefabRoot(obj);

            // スクリプトによる変更を有効にするためにPrefabとのリンクを切る
            if (original != null)
                PrefabUtility.DisconnectPrefabInstance(obj);
            
            hasChanged |= CheckGameObject(obj, path);

            // Prefabとのリンクを再接続
            if (original != null)
                PrefabUtility.ConnectGameObjectToPrefab(root, original);
        }

        if (hasChanged)
        {
            // UnityにSceneの変更を通知する（通知しないと保存されない）
            EditorSceneManager.MarkAllScenesDirty();

            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"Save Scene: {path}");
        }
        else
        {
            Debug.Log($"Scene has not changed: {path}");
        }
    }

    static void CheckPrefab(string path)
    {
        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        CheckGameObject(obj, path);
    }

    static bool CheckGameObject(GameObject obj, string path)
    {
        bool hasChanged = false;

        foreach (var component in obj.GetComponentsInChildren<Component>())
        {
            IAutomaticChecker checker = component as IAutomaticChecker;
            if (checker == null)
                continue;

            Debug.Log($"Check {path}: GameObjectName={component.gameObject.name}, ComponentName={component.GetType().Name}");
            hasChanged = true;
            checker.Check();

            // UnityにComponentの変更を通知する（通知しないと保存されない）
            EditorUtility.SetDirty(component);
        }

        return hasChanged;
    }

    static IEnumerable<string> GetSelectingFilePaths()
    {
        string[] guids = Selection.assetGUIDs;
        if (guids != null)
            return guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .SelectMany(assetPath => EnumerateFilePaths(assetPath));
        else
            return Enumerable.Empty<string>();
    }

    static IEnumerable<string> EnumerateFilePaths(string path)
    {
        if (File.Exists(path))
        {
            yield return path;
            yield break;
        }

        if (!Directory.Exists(path))
            yield break;

        foreach (string subPath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            yield return subPath;
    }
}

public interface IAutomaticChecker
{
    void Check();
}

#endif