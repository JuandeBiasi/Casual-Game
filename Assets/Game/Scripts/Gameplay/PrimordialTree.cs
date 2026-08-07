using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
public sealed class PrimordialTree : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuração")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField, Min(1)] private int manaPerTap = 1;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (manaSystem == null)
        {
            Debug.LogError(
                "ManaSystem não foi configurado na Árvore Primordial.",
                this
            );

            return;
        }

        manaSystem.AddMana(manaPerTap);
    }
}