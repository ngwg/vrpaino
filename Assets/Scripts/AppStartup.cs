using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class AppStartup : MonoBehaviour
{
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

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogError("[VRPiano] Camera permission denied");
            ShowStatus(
                "Camera access is required.\n\n" +
                "Please open Settings > Privacy & Security > Camera\n" +
                "and enable access for this app,\n" +
                "then relaunch.");
            yield break;
        }

        Debug.Log("[VRPiano] Camera permission granted, starting AR session");
        ShowStatus("Starting AR...");

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
            Debug.LogWarning($"[VRPiano] AR not ready after {timeout}s, state: {ARSession.state}");
            ShowStatus(
                $"AR could not start.\nState: {ARSession.state}\n\n" +
                "Ensure this device supports ARKit\n" +
                "and the camera is not in use by another app.");
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
