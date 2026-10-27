using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SlimeHabitat))]
public sealed class HabitatPopupTrigger : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private HabitatPopupUI popup;
    [SerializeField] private string habitatName;
    [SerializeField] private Color accentColor = Color.white;
    [SerializeField] private ClickScaleFeedback scaleFeedback;
    private SlimeHabitat habitat;

    private void Awake()
    {
        habitat = GetComponent<SlimeHabitat>();
        if (scaleFeedback == null) scaleFeedback = GetComponent<ClickScaleFeedback>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !isActiveAndEnabled ||
            popup == null || !popup.isActiveAndEnabled || habitat == null || !habitat.isActiveAndEnabled)
            return;

        popup.Open(habitat, habitatName, accentColor);
        if (scaleFeedback != null) scaleFeedback.Play();
    }
}
