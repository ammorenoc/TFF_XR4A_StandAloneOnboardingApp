using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isPressed = false;

    [Header("Quest3 part")]
    [SerializeField] private GameObject part;

    [Header("Highlight Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float scalePulseAmount = 0.05f; // 5% scale change

    [Header("Events")]
    public UnityEvent onToggledOn;
    public UnityEvent onToggledOff;

    private Renderer partRenderer;
    private Vector3 originalScale;
    private float lerpTime;

    void Awake()
    {
        if (part != null)
        {
            partRenderer = part.GetComponent<Renderer>();
            originalScale = part.transform.localScale;
            if (partRenderer != null)
            {
                partRenderer.material.color = normalColor;
            }
        }
    }

    void Update()
    {
        if (partRenderer == null) return;

        lerpTime += Time.deltaTime * pulseSpeed;
        float t = (Mathf.Sin(lerpTime) + 1f) / 2f; // oscillates between 0 and 1

        // Color pulsing
        if (!isPressed)
        {
            partRenderer.material.color = Color.Lerp(normalColor, highlightColor, t);
        }
        else
        {
            partRenderer.material.color = Color.Lerp(normalColor, pressedColor, t);
        }

        // Scale pulsing
        float scaleFactor = 1f + scalePulseAmount * t;
        part.transform.localScale = originalScale * scaleFactor;
    }

    void OnMouseDown()
    {
        Toggle();
    }

    public void Toggle()
    {
        isPressed = !isPressed;

        if (isPressed)
            onToggledOn?.Invoke();
        else
            onToggledOff?.Invoke();
    }

    public void SetActive(bool active)
    {
        if (isPressed == active) return;
        isPressed = active;

        if (isPressed)
            onToggledOn?.Invoke();
        else
            onToggledOff?.Invoke();
    }

    public bool IsActive() => isPressed;
}
