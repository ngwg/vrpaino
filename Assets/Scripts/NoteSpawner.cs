using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public ARPianoManager pianoManager;
    public float fallHeight = 0.8f;
    public float fallSpeed = 1.0f;
    public float bpm = 75f;

    // Twinkle Twinkle Little Star — white key indices (0=C, 1=D, 2=E, 3=F, 4=G, 5=A, 6=B, 7=C5...)
    static readonly int[] Song =
    {
        0, 0, 4, 4, 5, 5, 4,
        3, 3, 2, 2, 1, 1, 0,
        4, 4, 3, 3, 2, 2, 1,
        4, 4, 3, 3, 2, 2, 1,
        0, 0, 4, 4, 5, 5, 4,
        3, 3, 2, 2, 1, 1, 0,
    };

    static readonly float[] Beats =
    {
        1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,
        1,1,1,1,1,1,2,
    };

    void Start()
    {
        StartCoroutine(PlaySong());
    }

    IEnumerator PlaySong()
    {
        float beatDuration = 60f / bpm;

        // Wait until piano is placed on a surface
        while (pianoManager != null && !pianoManager.placed)
            yield return null;

        yield return new WaitForSeconds(1f);

        while (true)
        {
            for (int i = 0; i < Song.Length; i++)
            {
                SpawnNote(Song[i]);
                yield return new WaitForSeconds(beatDuration * Beats[i]);
            }
            yield return new WaitForSeconds(beatDuration * 2f);
        }
    }

    void SpawnNote(int keyIndex)
    {
        var keys = pianoManager.GetWhiteKeys();
        if (keys == null || keyIndex >= keys.Count) return;

        PianoKey key = keys[keyIndex];
        Vector3 spawnPos = key.transform.position + Vector3.up * fallHeight;

        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Note";
        block.transform.position = spawnPos;
        block.transform.localScale = new Vector3(0.026f, 0.055f, 0.018f);
        Destroy(block.GetComponent<BoxCollider>());

        var nb = block.AddComponent<NoteBlock>();
        nb.Init(keyIndex, key, fallSpeed);
    }
}
