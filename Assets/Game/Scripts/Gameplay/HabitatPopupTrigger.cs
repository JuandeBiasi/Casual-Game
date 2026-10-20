using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SlimeHabitat))]
public sealed class HabitatPopupTrigger : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private HabitatPopupUI popup;
    [SerializeField] private string habitatName;
    [SerializeField] private Color accentColor = Color.white;
    private SlimeHabitat habitat;

    private void Awake() => habitat = GetComponent<SlimeHabitat>();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && isActiveAndEnabled && popup != null)
            popup.Open(habitat, habitatName, accentColor);
    }
}
