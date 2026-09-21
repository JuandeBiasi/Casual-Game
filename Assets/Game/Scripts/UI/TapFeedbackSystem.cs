using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TapFeedbackSystem : MonoBehaviour
{
    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tapClip;

    [SerializeField, Range(0f, 1f)]
    private float tapVolume = 0.35f;

    [Header("Círculo do toque")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform rippleTransform;
    [SerializeField] private CanvasGroup rippleCanvasGroup;

    [SerializeField, Range(0f, 1f)]
    private float rippleOpacity = 0.45f;

    [SerializeField, Min(0.01f)]
    private float rippleDuration = 0.25f;

    [SerializeField, Min(0.01f)]
    private float startScale = 0.25f;

    [SerializeField, Min(0.01f)]
    private float endScale = 1.35f;

    private Coroutine rippleRoutine;

    private void Awake()
    {
        if (rippleCanvasGroup != null)
        {
            rippleCanvasGroup.alpha = 0f;
            rippleCanvasGroup.interactable = false;
            rippleCanvasGroup.blocksRaycasts = false;
        }

        if (rippleTransform != null)
        {
            rippleTransform.localScale =
                Vector3.one * startScale;
        }
    }

    private void Update()
    {
        if (TryGetTapPosition(out Vector2 screenPosition))
        {
            PlayFeedback(screenPosition);
        }
    }

    private static bool TryGetTapPosition(
        out Vector2 screenPosition
    )
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition =
                        touch.position.ReadValue();

                    return true;
                }
            }
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition =
                Mouse.current.position.ReadValue();

            return true;
        }

        screenPosition = default;
        return false;
    }

    private void PlayFeedback(Vector2 screenPosition)
    {
        if (audioSource != null && tapClip != null)
        {
            audioSource.PlayOneShot(
                tapClip,
                tapVolume
            );
        }

        if (targetCanvas == null ||
            rippleTransform == null ||
            rippleCanvasGroup == null)
        {
            return;
        }

        PositionRipple(screenPosition);

        if (rippleRoutine != null)
        {
            StopCoroutine(rippleRoutine);
        }

        rippleRoutine =
            StartCoroutine(AnimateRipple());
    }

    private void PositionRipple(Vector2 screenPosition)
    {
        RectTransform canvasRect =
            targetCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            return;
        }

        Camera uiCamera = null;

        if (targetCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = targetCanvas.worldCamera;
        }

        bool positionFound =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPosition
                );

        if (!positionFound)
        {
            return;
        }

        rippleTransform.anchoredPosition =
            localPosition;

        rippleTransform.SetAsLastSibling();
    }

    private IEnumerator AnimateRipple()
    {
        float elapsedTime = 0f;

        rippleCanvasGroup.alpha = rippleOpacity;

        rippleTransform.localScale =
            Vector3.one * startScale;

        while (elapsedTime < rippleDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / rippleDuration
            );

            float smoothProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            float currentScale = Mathf.Lerp(
                startScale,
                endScale,
                smoothProgress
            );

            rippleTransform.localScale =
                Vector3.one * currentScale;

            rippleCanvasGroup.alpha = Mathf.Lerp(
                rippleOpacity,
                0f,
                progress
            );

            yield return null;
        }

        rippleCanvasGroup.alpha = 0f;

        rippleTransform.localScale =
            Vector3.one * startScale;

        rippleRoutine = null;
    }

    private void OnDisable()
    {
        if (rippleRoutine != null)
        {
            StopCoroutine(rippleRoutine);
            rippleRoutine = null;
        }

        if (rippleCanvasGroup != null)
        {
            rippleCanvasGroup.alpha = 0f;
        }
    }

    private void OnValidate()
    {
        tapVolume = Mathf.Clamp01(tapVolume);
        rippleOpacity = Mathf.Clamp01(rippleOpacity);

        rippleDuration = Mathf.Max(
            0.01f,
            rippleDuration
        );

        startScale = Mathf.Max(
            0.01f,
            startScale
        );

        endScale = Mathf.Max(
            startScale,
            endScale
        );
    }
}