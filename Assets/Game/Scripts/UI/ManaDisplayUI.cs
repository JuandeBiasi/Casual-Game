using TMPro;
using UnityEngine;

public sealed class ManaDisplayUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private TMP_Text manaAmountText;

    private void OnEnable()
    {
        if (manaSystem == null)
        {
            Debug.LogError("ManaSystem não foi configurado.", this);
            return;
        }

        manaSystem.ManaChanged += UpdateDisplay;
        UpdateDisplay(manaSystem.CurrentMana);
    }

    private void OnDisable()
    {
        if (manaSystem != null)
        {
            manaSystem.ManaChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(long amount)
    {
        if (manaAmountText == null)
        {
            return;
        }

        manaAmountText.text = $"Mana: {amount:N0}";
    }
}