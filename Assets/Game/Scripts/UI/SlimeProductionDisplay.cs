using TMPro;
using UnityEngine;

public sealed class SlimeProductionDisplay : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SlimeManaProducer manaProducer;
    [SerializeField] private TMP_Text productionText;

    private void OnEnable()
    {
        if (manaProducer == null)
        {
            Debug.LogError(
                "SlimeManaProducer não foi configurado.",
                this
            );

            return;
        }

        manaProducer.ProductionChanged += UpdateDisplay;

        UpdateDisplay(manaProducer.TotalManaPerSecond);
    }

    private void OnDisable()
    {
        if (manaProducer != null)
        {
            manaProducer.ProductionChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(long manaPerSecond)
    {
        if (productionText == null)
        {
            return;
        }

        productionText.text = $"+{manaPerSecond:N0} MANA/S";
    }
}