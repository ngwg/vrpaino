using UnityEngine;

public class PianoKey : MonoBehaviour
{
    public int noteIndex;
    public bool isBlackKey;

    Color baseColor;
    Color highlightColor = new Color(0.2f, 0.8f, 1f);
    Color pressColor = new Color(1f, 0.4f, 0.1f);

    Renderer rend;
    float highlightTimer;
    bool pressed;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        baseColor = isBlackKey ? Color.black : Color.white;
        rend.material.color = baseColor;
    }

    void Update()
    {
        if (highlightTimer > 0)
        {
            highlightTimer -= Time.deltaTime;
            if (highlightTimer <= 0 && !pressed)
                rend.material.color = baseColor;
        }
    }

    public void Highlight(bool on)
    {
        if (on)
        {
            highlightTimer = 0.4f;
            rend.material.color = highlightColor;
        }
        else
        {
            rend.material.color = baseColor;
        }
    }

    public void Press()
    {
        pressed = true;
        rend.material.color = pressColor;
        CancelInvoke(nameof(Release));
        Invoke(nameof(Release), 0.25f);
    }

    void Release()
    {
        pressed = false;
        rend.material.color = baseColor;
    }
}
