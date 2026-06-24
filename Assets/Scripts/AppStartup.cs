using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public class AppStartup : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int _CameraPermissionStatus();

    [DllImport("__Internal")]
    private static extern void _RequestCameraPermission();
#endif

    ARSession arSession;
    Text statusText;

    void Awake()
    {
        arSession = GetComponent<ARSession>();
        if (arSession != null && arSession.enabled)
            arSession.enabled = false;
    }

    IEnumerator Start()
    {
        statusText = FindStatusText();
        ShowStatus("Requesting camera access...");

#if UNITY_IOS && !UNITY_EDITOR
        int status = _CameraPermissionStatus();

        if (status == 0)
        {
            _RequestCameraPermission();
            while (_CameraPermissionStatus() == 0)
                yield return new WaitForSeconds(0.2f);
            status = _CameraPermissionStatus();
        }

        if (status != 1)
        {
            ShowStatus(
                "Camera access is required.\n\n" +
                "Go to Settings > Privacy & Security > Camera\n" +
                "and enable access for this app,\n" +
                "then relaunch.");
            yield break;
        }
#else
        yield return null;
#endif

        Debug.Log("[VRPiano] Camera permission granted");
        ShowStatus("Starting AR...");

        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null)
        {
            ShowStatus("XR settings not found.\nBuild may be misconfigured.");
            yield break;
        }

        var xrManager = xrSettings.Manager;
        if (xrManager == null)
        {
            ShowStatus("XR manager not found.\nBuild may be misconfigured.");
            yield break;
        }

        // Deinitialize first in case auto-init tried and failed
        if (xrManager.isInitializationComplete)
        {
            Debug.Log("[VRPiano] Deinitializing previous XR attempt...");
            xrManager.DeinitializeLoader();
        }

        Debug.Log($"[VRPiano] Initializing XR loader (loaders: {xrManager.activeLoaders.Count})...");
        yield return xrManager.InitializeLoader();

        if (xrManager.activeLoader == null)
        {
            Debug.LogError("[VRPiano] XR loader init failed");
            ShowStatus(
                "ARKit failed to initialize.\n\n" +
                "Loaders configured: " + xrManager.activeLoaders.Count + "\n" +
                "Device: " + SystemInfo.deviceModel + "\n" +
                "OS: " + SystemInfo.operatingSystem);
            yield break;
        }

        Debug.Log($"[VRPiano] XR loader active: {xrManager.activeLoader.name}");
        xrManager.StartSubsystems();

        if (arSession != null)
            arSession.enabled = true;

        float timeout = 5f;
        float elapsed = 0f;
        while (ARSession.state < ARSessionState.Ready && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[VRPiano] AR state: {ARSession.state}");
        ShowStatus("Look at a flat surface\nto place the piano");

        // Wait until piano is placed, then hide
        var piano = FindAnyObjectByType<ARPianoManager>();
        if (piano != null)
        {
            while (!piano.placed)
                yield return null;
            ShowStatus("Piano placed!");
            yield return new WaitForSeconds(1.5f);
        }
        HideStatus();
    }

    Text FindStatusText()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        return canvas != null ? canvas.GetComponentInChildren<Text>() : null;
    }

    void ShowStatus(string message)
    {
        Debug.Log($"[VRPiano] {message}");
        if (statusText != null)
        {
            statusText.text = message;
            statusText.transform.parent.gameObject.SetActive(true);
        }
    }

    void HideStatus()
    {
        if (statusText != null)
            statusText.transform.parent.gameObject.SetActive(false);
    }
}
