using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public static class SceneSetup
{
    [MenuItem("VRPiano/Build AR Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.GetActiveScene();

        // Clear scene
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // AR Session (disabled — AppStartup enables it after camera permission)
        // ARInputManager must be on a separate always-enabled GO —
        // ARSession starts disabled and would prevent ARInputManager.OnEnable
        var inputManagerGo = new GameObject("AR Input Manager");
        inputManagerGo.AddComponent<ARInputManager>();

        var sessionGo = new GameObject("AR Session");
        var arSession = sessionGo.AddComponent<ARSession>();
        arSession.enabled = false;
        sessionGo.AddComponent<AppStartup>();

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
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.01f;
        var tpd = cameraGo.AddComponent<TrackedPoseDriver>();
        // Match exactly what AR Foundation's XROriginCreateUtil sets up:
        // two bindings per action (XRHMD + HandheldARInputDevice)
        var posAction = new InputAction("Position", InputActionType.Value,
            binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
        var rotAction = new InputAction("Rotation", InputActionType.Value,
            binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
        tpd.positionInput = new InputActionProperty(posAction);
        tpd.rotationInput = new InputActionProperty(rotAction);
        tpd.updateType = TrackedPoseDriver.UpdateType.BeforeRender;
        cameraGo.AddComponent<ARCameraManager>();
        cameraGo.AddComponent<ARCameraBackground>();
        xrOrigin.CameraFloorOffsetObject = offsetGo;
        xrOrigin.Camera = cam;
        // 0 = no floor offset (phone AR, not VR floor mode)
        xrOrigin.CameraYOffset = 0f;

        // AR Plane Detection (disabled until XR subsystems are ready)
        var planeManager = originGo.AddComponent<ARPlaneManager>();
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        planeManager.enabled = false;
        var raycastMgr = originGo.AddComponent<ARRaycastManager>();
        raycastMgr.enabled = false;
        var anchorMgr = originGo.AddComponent<ARAnchorManager>();
        anchorMgr.enabled = false;

        // Directional Light
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Piano Root
        var pianoRoot = new GameObject("Piano Root");
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

        // Status UI Canvas
        var canvasGo = new GameObject("Status Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Background");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f);

        var textGo = new GameObject("StatusText");
        textGo.transform.SetParent(panelGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.3f);
        textRect.anchorMax = new Vector2(0.9f, 0.7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = "Initializing...";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        // Save
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ARPiano.unity");
        AssetDatabase.Refresh();
        Debug.Log("[VRPiano] Scene built and saved.");
    }
}
