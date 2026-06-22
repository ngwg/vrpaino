using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public static class SceneSetup
{
    [MenuItem("VRPiano/Build AR Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // Clear scene
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // AR Session
        var sessionGo = new GameObject("AR Session");
        sessionGo.AddComponent<ARSession>();

        // XR Origin
        var originGo = new GameObject("XR Origin");
        var xrOrigin = originGo.AddComponent<XROrigin>();

        // Camera Offset (required child of XR Origin)
        var offsetGo = new GameObject("Camera Offset");
        offsetGo.transform.SetParent(originGo.transform, false);

        // AR Camera
        var cameraGo = new GameObject("Main Camera");
        cameraGo.transform.SetParent(offsetGo.transform, false);
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.01f;
        cameraGo.AddComponent<ARCameraManager>();
        cameraGo.AddComponent<ARCameraBackground>();
        xrOrigin.Camera = cam;

        // Directional Light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Piano Root
        var pianoRoot = new GameObject("Piano Root");
        pianoRoot.transform.position = new Vector3(0, -0.28f, 0.7f);
        var manager = pianoRoot.AddComponent<ARPianoManager>();
        manager.cameraTransform = cameraGo.transform;

        // Note Spawner
        var spawnerGo = new GameObject("Note Spawner");
        spawnerGo.transform.SetParent(pianoRoot.transform, false);
        var spawner = spawnerGo.AddComponent<NoteSpawner>();
        spawner.pianoManager = manager;
        spawner.fallHeight = 0.8f;
        spawner.fallSpeed = 1.0f;
        spawner.bpm = 75f;

        // Hand Interaction
        var handGo = new GameObject("Hand Interaction");
        var hand = handGo.AddComponent<HandInteraction>();
        hand.pianoManager = manager;

        // Save
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ARPiano.unity");
        AssetDatabase.Refresh();
        Debug.Log("[VRPiano] Scene built and saved.");
    }
}
