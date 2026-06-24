using System.Collections.Generic;
using UnityEngine;

public class ARPianoManager : MonoBehaviour
{
    public Transform cameraTransform;
    public float distanceFromCamera = 0.5f;
    public float heightOffset = -0.3f;
    public bool placed;

    readonly List<PianoKey> whiteKeys = new();
    readonly List<PianoKey> allKeys = new();

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
        PlacePiano();
    }

    void PlacePiano()
    {
        Vector3 pos = cameraTransform.position
            + cameraTransform.forward * distanceFromCamera
            + Vector3.up * heightOffset;

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        placed = true;
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
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        rend.material.color = black ? Color.black : Color.white;

        var key = go.AddComponent<PianoKey>();
        key.noteIndex = index;
        key.isBlackKey = black;
        return key;
    }

    public List<PianoKey> GetWhiteKeys() => whiteKeys;
    public List<PianoKey> GetAllKeys() => allKeys;
}
