using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
public sealed class GrassHabitat : MonoBehaviour, IPointerClickHandler
{
    [Header("Configuração")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private long restorationCost = 150;

    [Header("Visuais")]
    [SerializeField] private GameObject ruinedVisual;
    [SerializeField] private GameObject restoredVisual;
    [SerializeField] private GameObject restorePrompt;

    private bool isRestored;

    public bool IsRestored => isRestored;
    public event Action Restored;

    private void Awake()
    {
        if (manaSystem == null)
        {
            Debug.LogError(
                "ManaSystem não foi configurado no habitat de grama.",
                this
            );
        }

        UpdateVisuals();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        TryRestore();
    }

    public bool TryRestore()
    {
        if (isRestored)
        {
            return false;
        }

        if (manaSystem == null)
        {
            return false;
        }

        if (!manaSystem.TrySpendMana(restorationCost))
        {
            Debug.Log(
                $"Mana insuficiente. São necessárias {restorationCost} manas.",
                this
            );

            return false;
        }

        isRestored = true;

        UpdateVisuals();
        Restored?.Invoke();

        Debug.Log("Habitat do slime de grama restaurado!", this);

        return true;
    }

    private void UpdateVisuals()
    {
        if (ruinedVisual != null)
        {
            ruinedVisual.SetActive(!isRestored);
        }

        if (restoredVisual != null)
        {
            restoredVisual.SetActive(isRestored);
        }

        if (restorePrompt != null)
        {
            restorePrompt.SetActive(!isRestored);
        }
    }
}