using UnityEngine;

public class NoteBlock : MonoBehaviour
{
    public int noteIndex;
    public float fallSpeed = 1.2f;

    PianoKey targetKey;
    bool triggered;

    static readonly Color[] NoteColors =
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(1f, 0.6f, 0.1f),
        new Color(1f, 1f, 0.1f),
        new Color(0.2f, 1f, 0.2f),
        new Color(0.1f, 0.7f, 1f),
        new Color(0.5f, 0.1f, 1f),
        new Color(1f, 0.2f, 0.8f),
        new Color(0.8f, 0.5f, 1f),
        new Color(0.2f, 1f, 0.8f),
        new Color(1f, 0.8f, 0.3f),
        new Color(0.3f, 1f, 0.5f),
        new Color(0.9f, 0.3f, 0.5f),
    };

    public void Init(int note, PianoKey key, float speed)
    {
        noteIndex = note;
        targetKey = key;
        fallSpeed = speed;
        var rend = GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        rend.material.color = NoteColors[note % NoteColors.Length];
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);

        if (!triggered && targetKey != null)
        {
            if (Vector3.Distance(transform.position, targetKey.transform.position) < 0.06f)
            {
                triggered = true;
                targetKey.Press();
                Invoke(nameof(DestroySelf), 0.15f);
            }
        }

        if (transform.position.y < targetKey.transform.position.y - 0.3f)
            Destroy(gameObject);
    }

    void DestroySelf() => Destroy(gameObject);
}
