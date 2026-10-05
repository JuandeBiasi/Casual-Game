using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SlimeHabitat))]
public sealed class SlimeManaProducer : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ManaSystem manaSystem;

    [Header("Produção por raridade")]
    [SerializeField, Min(1)] private long commonManaPerSecond = 2;
    [SerializeField, Min(1)] private long rareManaPerSecond = 5;
    [SerializeField, Min(1)] private long legendaryManaPerSecond = 10;

    private SlimeHabitat grassHabitat;
    private Coroutine productionCoroutine;
    private WaitForSeconds waitForProduction;

    public long TotalManaPerSecond =>
        CalculateTotalManaPerSecond();

    public event Action<long> ProductionChanged;

    private void Awake()
    {
        grassHabitat = GetComponent<SlimeHabitat>();
        waitForProduction = new WaitForSeconds(1f);

        if (manaSystem == null)
        {
            Debug.LogError(
                "ManaSystem não foi configurado no produtor de mana.",
                this
            );
        }
    }

    private void HandleProgressLoaded()
    {
        if (grassHabitat.HasSlime)
        {
            StartProduction();
        }
        else
        {
            StopProduction();
        }

        NotifyProductionChanged();
    }

    private void OnEnable()
    {
        grassHabitat.SlimePurchased += HandleSlimePurchased;
        grassHabitat.HabitatUpgraded += HandleHabitatUpgraded;
        grassHabitat.ProgressLoaded += HandleProgressLoaded;

        if (grassHabitat.HasSlime)
        {
            StartProduction();
        }
    }

    private void OnDisable()
    {
        if (grassHabitat != null)
        {
            grassHabitat.SlimePurchased -= HandleSlimePurchased;
            grassHabitat.HabitatUpgraded -= HandleHabitatUpgraded;
            grassHabitat.ProgressLoaded -= HandleProgressLoaded;
        }

        StopProduction();
    }

    private void HandleSlimePurchased(SlimeRarity purchasedRarity)
    {
        StartProduction();
        NotifyProductionChanged();
    }

    private void HandleHabitatUpgraded(int newLevel)
    {
        NotifyProductionChanged();
    }
    private void NotifyProductionChanged()
    {
        ProductionChanged?.Invoke(TotalManaPerSecond);
    }

    private void StartProduction()
    {
        if (productionCoroutine != null || manaSystem == null)
        {
            return;
        }

        productionCoroutine = StartCoroutine(ProduceMana());
    }

    private void StopProduction()
    {
        if (productionCoroutine == null)
        {
            return;
        }

        StopCoroutine(productionCoroutine);
        productionCoroutine = null;
    }

    private IEnumerator ProduceMana()
    {
        while (true)
        {
            yield return waitForProduction;

            long totalMana = TotalManaPerSecond;

            if (totalMana > 0)
            {
                manaSystem.AddMana(totalMana);
            }
        }
    }

    private long CalculateTotalManaPerSecond()
    {
        long totalMana = 0;

        foreach (SlimeRarity rarity in grassHabitat.Slimes)
        {
            totalMana += GetManaPerSecond(rarity);
        }

        return totalMana * grassHabitat.ProductionMultiplier;
    }

    private long GetManaPerSecond(SlimeRarity rarity)
    {
        switch (rarity)
        {
            case SlimeRarity.Common:
                return commonManaPerSecond;

            case SlimeRarity.Rare:
                return rareManaPerSecond;

            case SlimeRarity.Legendary:
                return legendaryManaPerSecond;

            default:
                return 0;
        }
    }
}
