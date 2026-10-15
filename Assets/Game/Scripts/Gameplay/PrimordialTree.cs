using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
public sealed class PrimordialTree : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuração")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField, Min(1)] private int manaPerTap = 1;

    public event Action<int> ManaTapped;

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

        int gainedMana = manaPerTap;
        if (gainedMana <= 0)
        {
            return;
        }

        manaSystem.AddMana(gainedMana);
        ManaTapped?.Invoke(gainedMana);
    }
}
