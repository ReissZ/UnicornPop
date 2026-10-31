using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Unicorn Pop/Create Main Scene")]
    public static void CreateMainScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        new GameObject("UnicornPop").AddComponent<UnicornPopGame>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }
}
