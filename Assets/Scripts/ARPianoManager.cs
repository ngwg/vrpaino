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
    ARAnchor currentAnchor;

    GameObject placementIndicator;
    float stableTimer;
    float totalScanTime;
    Vector3 lastHitPosition;
    const float StableThreshold = 0.05f;
    const float StableTimeRequired = 2f;
    const float FallbackTimeout = 15f;

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

        BuildPiano();
        SetPianoVisible(false);
        CreatePlacementIndicator();
    }

    void Update()
    {
        if (placed) return;

        // Lazy-find managers (they start disabled, AppStartup enables them after XR is ready)
        if (raycastManager == null)
            raycastManager = FindAnyObjectByType<ARRaycastManager>();
        if (planeManager == null)
            planeManager = FindAnyObjectByType<ARPlaneManager>();
        if (anchorManager == null)
            anchorManager = FindAnyObjectByType<ARAnchorManager>();

        totalScanTime += Time.deltaTime;

        // Fallback: if no surface found after timeout, place in front of camera
        if (totalScanTime >= FallbackTimeout)
        {
            Debug.Log("[VRPiano] Plane detection timed out — using fallback placement");
            FallbackPlace();
            return;
        }

        if (raycastManager == null || !raycastManager.enabled) return;

        var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            var hit = raycastHits[0];
            var hitPos = hit.pose.position;

            if (!placementIndicator.activeSelf)
                placementIndicator.SetActive(true);

            placementIndicator.transform.position = hitPos;
            placementIndicator.transform.rotation = Quaternion.Euler(0, hit.pose.rotation.eulerAngles.y, 0);

            if (Vector3.Distance(hitPos, lastHitPosition) < StableThreshold)
            {
                stableTimer += Time.deltaTime;
                if (stableTimer >= StableTimeRequired)
                    PlacePianoAt(hit);
            }
            else
            {
                stableTimer = 0f;
                lastHitPosition = hitPos;
            }
        }
        else
        {
            if (placementIndicator != null && placementIndicator.activeSelf)
                placementIndicator.SetActive(false);
            stableTimer = 0f;
        }

        // Tap to place immediately
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(Input.GetTouch(0).position, raycastHits, TrackableType.PlaneWithinPolygon))
                PlacePianoAt(raycastHits[0]);
        }
    }

    async void PlacePianoAt(ARRaycastHit hit)
    {
        // Guard against double-placement while async runs
        if (placed) return;
        placed = true;

        if (placementIndicator != null)
            Destroy(placementIndicator);

        float yaw = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)).eulerAngles.y;

        var pose = new Pose(
            hit.pose.position + Vector3.up * (WhiteKeyHeight / 2f),
            Quaternion.Euler(0, yaw, 0));

        // Stop plane detection now
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        if (anchorManager != null)
        {
            // TryAddAnchorAsync is the correct API in AR Foundation 6 —
            // AddComponent<ARAnchor>() silently fails and disables the GO in 6.0.3
            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (result.status.IsSuccess())
            {
                currentAnchor = result.value;
                // Correct hierarchy: piano is a child of the anchor GO so ARKit moves it
                transform.SetParent(currentAnchor.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                Debug.Log($"[VRPiano] Anchor created, tracking={currentAnchor.trackingState}");
            }
            else
            {
                Debug.LogWarning($"[VRPiano] Anchor failed ({result.status}), falling back to free placement");
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }
        else
        {
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        SetPianoVisible(true);
        Debug.Log($"[VRPiano] Piano placed at {pose.position}");
    }

    void FallbackPlace()
    {
        if (placed) return;
        placed = true;

        if (placementIndicator != null)
            Destroy(placementIndicator);

        float yaw = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up)).eulerAngles.y;

        transform.position = cameraTransform.position
            + cameraTransform.forward * 0.5f
            + Vector3.up * -0.3f;
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        SetPianoVisible(true);
        Debug.Log("[VRPiano] Piano fallback-placed (no surface detected)");
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
        rend.material.color = new Color(0.2f, 1f, 0.4f, 1f);
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
                if (!black)
                {
                    float xPos = startX + (whiteIndex + 0.5f) * WhiteKeyWidth;
                    var key = CreateKey(xPos, 0f, 0f, WhiteKeyWidth - 0.002f, WhiteKeyHeight, WhiteKeyDepth, false, noteIndex);
                    whiteKeys.Add(key);
                    allKeys.Add(key);
                    whiteIndex++;
                }
                else
                {
                    float xPos = startX + (whiteIndex - 0.5f) * WhiteKeyWidth;
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
