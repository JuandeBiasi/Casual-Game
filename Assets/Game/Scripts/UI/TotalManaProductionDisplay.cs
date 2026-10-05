using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class TotalManaProductionDisplay : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SlimeManaProducer[] manaProducers;
    [SerializeField] private TMP_Text productionText;

    private readonly HashSet<SlimeManaProducer> subscribedProducers =
        new HashSet<SlimeManaProducer>();

    private bool started;

    private void Start()
    {
        started = true;
        Subscribe();
        RefreshDisplay();
    }

    private void OnEnable()
    {
        if (!started)
        {
            return;
        }

        Subscribe();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();

        if (manaProducers == null)
        {
            return;
        }

        foreach (SlimeManaProducer producer in manaProducers)
        {
            if (producer != null && subscribedProducers.Add(producer))
            {
                producer.ProductionChanged += HandleProductionChanged;
            }
        }
    }

    private void Unsubscribe()
    {
        foreach (SlimeManaProducer producer in subscribedProducers)
        {
            if (producer != null)
            {
                producer.ProductionChanged -= HandleProductionChanged;
            }
        }

        subscribedProducers.Clear();
    }

    private void HandleProductionChanged(long manaPerSecond)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (productionText == null)
        {
            return;
        }

        long totalManaPerSecond = 0;

        foreach (SlimeManaProducer producer in subscribedProducers)
        {
            if (producer != null)
            {
                totalManaPerSecond += producer.TotalManaPerSecond;
            }
        }

        productionText.text = $"+{totalManaPerSecond:N0} MANA/S";
    }
}
