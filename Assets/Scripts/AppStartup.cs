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
            Debug.LogError("[VRPiano] Camera permission denied");
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

        // Ensure XR is initialized
        var xrSettings = XRGeneralSettings.Instance;
        if (xrSettings == null)
        {
            Debug.LogError("[VRPiano] XRGeneralSettings.Instance is null");
            ShowStatus("XR settings not found.\nThe build may be misconfigured.");
            yield break;
        }

        var xrManager = xrSettings.Manager;
        if (xrManager == null)
        {
            Debug.LogError("[VRPiano] XRManagerSettings is null");
            ShowStatus("XR manager not found.\nThe build may be misconfigured.");
            yield break;
        }

        if (xrManager.activeLoader == null)
        {
            Debug.Log("[VRPiano] No active XR loader, initializing manually...");
            yield return xrManager.InitializeLoader();
        }

        if (xrManager.activeLoader == null)
        {
            Debug.LogError("[VRPiano] XR loader failed to initialize");
            ShowStatus("ARKit failed to load.\nCheck device compatibility.");
            yield break;
        }

        Debug.Log($"[VRPiano] XR loader active: {xrManager.activeLoader.name}");
        xrManager.StartSubsystems();

        // Now enable ARSession
        if (arSession != null)
            arSession.enabled = true;

        float timeout = 5f;
        float elapsed = 0f;
        while (ARSession.state < ARSessionState.Ready && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ARSession.state >= ARSessionState.Ready)
        {
            Debug.Log($"[VRPiano] AR ready, state: {ARSession.state}");
            HideStatus();
        }
        else
        {
            Debug.LogWarning($"[VRPiano] AR state after {timeout}s: {ARSession.state}");
            ShowStatus(
                $"AR state: {ARSession.state}\n\n" +
                "Trying to continue anyway...");
            yield return new WaitForSeconds(2f);
            HideStatus();
        }
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
