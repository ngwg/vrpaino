using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AppStartup : MonoBehaviour
{
    IEnumerator Start()
    {
        // Request camera permission on iOS
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("Camera permission granted");
        }
        else
        {
            Debug.LogWarning("Camera permission denied");
        }
    }
}
