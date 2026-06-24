using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPianoManager : MonoBehaviour
{
    public Transform cameraTransform;
    public bool placed;

    ARRaycastManager raycastManager;
    ARPlaneManager planeManager;
    ARAnchorManager anchorManager;
    GameObject placementIndicator;
    float stableTimer;
    Vector3 lastHitPosition;
    const float StableThreshold = 0.05f;
    const float StableTimeRequired = 2f;

    readonly List<PianoKey> whiteKeys = new();
    readonly List<PianoKey> allKeys = new();
    readonly List<ARRaycastHit> raycastHits = new();

    static readonly bool[] IsBlack = { false, true, false, true, false, false, true, false, true, false, true, false };

    const int Octaves = 2;
    const float WhiteKeyWidth = 0.032f;
    const float WhiteKeyHeight = 0.012f;
    const float WhiteKeyDepth = 0.14f;
    const float BlackKeyWidth = 0.020f;
    const float BlackKeyHeight = 0.016f;
    const float BlackKeyDepth = 0.09f;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        raycastManager = FindAnyObjectByType<ARRaycastManager>();
        planeManager = FindAnyObjectByType<ARPlaneManager>();
        anchorManager = FindAnyObjectByType<ARAnchorManager>();

        BuildPiano();
        SetPianoVisible(false);
        CreatePlacementIndicator();
    }

    void Update()
    {
        if (placed) return;

        var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager != null && raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];
            var hitPos = hit.pose.position;
            var hitRot = hit.pose.rotation;

            if (!placementIndicator.activeSelf)
                placementIndicator.SetActive(true);

            placementIndicator.transform.position = hitPos;
            placementIndicator.transform.rotation = Quaternion.Euler(0, hitRot.eulerAngles.y, 0);

            if (Vector3.Distance(hitPos, lastHitPosition) < StableThreshold)
            {
                stableTimer += Time.deltaTime;
                if (stableTimer >= StableTimeRequired)
                {
                    PlacePianoAt(hitPos, hitRot);
                    return;
                }
            }
            else
            {
                stableTimer = 0f;
                lastHitPosition = hitPos;
            }
        }
        else
        {
            if (placementIndicator.activeSelf)
                placementIndicator.SetActive(false);
            stableTimer = 0f;
        }

        // Tap to place as backup
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (raycastManager != null && raycastManager.Raycast(Input.GetTouch(0).position, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                var hit = raycastHits[0];
                PlacePianoAt(hit.pose.position, hit.pose.rotation);
            }
        }
    }

    void PlacePianoAt(Vector3 position, Quaternion rotation)
    {
        Destroy(placementIndicator);

        float yawToCamera = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)).eulerAngles.y;

        transform.position = position + Vector3.up * (WhiteKeyHeight / 2f);
        transform.rotation = Quaternion.Euler(0, yawToCamera, 0);

        // Anchor to keep piano locked to this physical spot
        if (gameObject.GetComponent<ARAnchor>() == null)
            gameObject.AddComponent<ARAnchor>();

        SetPianoVisible(true);
        placed = true;

        // Disable plane detection to save performance
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        Debug.Log($"[VRPiano] Piano placed at {position}");
    }

    void SetPianoVisible(bool visible)
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<NoteSpawner>() != null) continue;
            child.gameObject.SetActive(visible);
        }
    }

    void CreatePlacementIndicator()
    {
        placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        placementIndicator.name = "PlacementIndicator";
        placementIndicator.transform.localScale = new Vector3(0.15f, 0.002f, 0.15f);
        Destroy(placementIndicator.GetComponent<Collider>());

        var rend = placementIndicator.GetComponent<Renderer>();
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? rend.material.shader;
        rend.material = new Material(shader);
        rend.material.color = new Color(0.2f, 1f, 0.4f, 0.7f);

        placementIndicator.SetActive(false);
    }

    void BuildPiano()
    {
        int totalWhiteKeys = 7 * Octaves;
        float totalWidth = totalWhiteKeys * WhiteKeyWidth;
        float startX = -totalWidth / 2f;

        int whiteIndex = 0;
        int noteIndex = 0;

        for (int octave = 0; octave < Octaves; octave++)
        {
            for (int semi = 0; semi < 12; semi++)
            {
                bool black = IsBlack[semi];
                float xPos;

                if (!black)
                {
                    xPos = startX + (whiteIndex + 0.5f) * WhiteKeyWidth;
                    var key = CreateKey(xPos, 0f, 0f, WhiteKeyWidth - 0.002f, WhiteKeyHeight, WhiteKeyDepth, false, noteIndex);
                    whiteKeys.Add(key);
                    allKeys.Add(key);
                    whiteIndex++;
                }
                else
                {
                    float lastWhiteX = startX + (whiteIndex - 0.5f) * WhiteKeyWidth;
                    xPos = lastWhiteX;
                    var key = CreateKey(xPos, BlackKeyHeight / 2f, (WhiteKeyDepth - BlackKeyDepth) / 2f - 0.01f,
                        BlackKeyWidth, BlackKeyHeight, BlackKeyDepth, true, noteIndex);
                    allKeys.Add(key);
                }

                noteIndex++;
            }
        }
    }

    PianoKey CreateKey(float x, float yOffset, float zOffset, float w, float h, float d, bool black, int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = black ? $"Black_{index}" : $"White_{index}";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x, yOffset, zOffset);
        go.transform.localScale = new Vector3(w, h, d);
        Destroy(go.GetComponent<BoxCollider>());

        var rend = go.GetComponent<Renderer>();
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? rend.material.shader;
        rend.material = new Material(shader);
        rend.material.color = black ? Color.black : Color.white;

        var key = go.AddComponent<PianoKey>();
        key.noteIndex = index;
        key.isBlackKey = black;
        return key;
    }

    public List<PianoKey> GetWhiteKeys() => whiteKeys;
    public List<PianoKey> GetAllKeys() => allKeys;
}
