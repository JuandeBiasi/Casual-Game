using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BoxCollider))]
public sealed class GrassHabitat : MonoBehaviour, IPointerClickHandler
{
    private enum HabitatState
    {
        Ruined,
        RestoredEmpty,
        Occupied
    }

    [Header("Mana")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private long restorationCost = 150;
    [SerializeField, Min(1)] private long firstSlimeCost = 100;

    [Header("Habitat")]
    [SerializeField, Min(1)] private int levelOneSlimeCapacity = 4;
    [SerializeField, Min(1)] private int levelTwoSlimeCapacity = 6;
    [SerializeField, Min(1)] private long levelTwoUpgradeCost = 650;
    [SerializeField] private GameObject ruinedVisual;
    [SerializeField] private GameObject restoredVisual;
    [SerializeField] private TMP_Text interactionPromptText;

    [Header("Slimes")]
    [SerializeField] private SlimeVisualSlot[] slimeVisualSlots;

    private HabitatState habitatState = HabitatState.Ruined;
    private readonly List<SlimeRarity> slimes = new List<SlimeRarity>();
    private int habitatLevel = 1;

    public bool IsRestored =>
        habitatState != HabitatState.Ruined;

    public bool HasSlime =>
        slimes.Count > 0;

    public int SlimeCount => slimes.Count;
    public int HabitatLevel => habitatLevel;

    public int MaxSlimeCapacity =>
        habitatLevel == 1
            ? levelOneSlimeCapacity
            : levelTwoSlimeCapacity;

    public int ProductionMultiplier =>
        habitatLevel == 1 ? 1 : 2;
    public bool HasAvailableSlimeSlot =>
        IsRestored && SlimeCount < MaxSlimeCapacity;
    public bool CanUpgrade =>
    IsRestored &&
    habitatLevel == 1 &&
    SlimeCount >= levelOneSlimeCapacity;

    public long NextSlimeCost => CalculateSlimeCost(SlimeCount);

    public IReadOnlyList<SlimeRarity> Slimes => slimes;

    public event Action Restored;
    public event Action<SlimeRarity> SlimePurchased;
    public event Action<int> HabitatUpgraded;

    private void Awake()
    {
        levelOneSlimeCapacity = Mathf.Max(1, levelOneSlimeCapacity);

        levelTwoSlimeCapacity = Mathf.Max(
            levelOneSlimeCapacity + 1,
            levelTwoSlimeCapacity
        );

        levelTwoUpgradeCost = Math.Max(1L, levelTwoUpgradeCost);
        habitatLevel = 1;
        firstSlimeCost = Math.Max(1L, firstSlimeCost);
        slimes.Clear();

        if (manaSystem == null)
        {
            Debug.LogError(
                "ManaSystem não foi configurado no habitat.",
                this
            );
        }

        habitatState = HabitatState.Ruined;
        UpdatePresentation();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        switch (habitatState)
        {
            case HabitatState.Ruined:
                TryRestore();
                break;

            case HabitatState.RestoredEmpty:
                TryBuySlime();
                break;

            case HabitatState.Occupied:
                if (CanUpgrade)
                {
                    TryUpgradeHabitat();
                }
                else
                {
                    TryBuySlime();
                }

                break;
        }
    }

    private bool TryRestore()
    {
        if (manaSystem == null)
        {
            return false;
        }

        if (!manaSystem.TrySpendMana(restorationCost))
        {
            Debug.Log(
                $"São necessárias {restorationCost} manas para restaurar.",
                this
            );

            return false;
        }

        habitatState = HabitatState.RestoredEmpty;

        UpdatePresentation();
        Restored?.Invoke();

        return true;
    }

    private bool TryBuySlime()
    {
        if (manaSystem == null)
        {
            return false;
        }

        if (!HasAvailableSlimeSlot)
        {
            Debug.Log("O habitat já está com a capacidade máxima.", this);
            return false;
        }

        long slimeCost = NextSlimeCost;

        if (!manaSystem.TrySpendMana(slimeCost))
        {
            Debug.Log(
                $"São necessárias {slimeCost} manas para comprar o próximo slime.",
                this
            );

            return false;
        }



        SlimeRarity purchasedSlimeRarity = RollSlimeRarity();
        slimes.Add(purchasedSlimeRarity);
        habitatState = HabitatState.Occupied;

        UpdatePresentation();
        SlimePurchased?.Invoke(purchasedSlimeRarity);

        Debug.Log(
            $"Slime obtido: {GetRarityLabel(purchasedSlimeRarity)}! " +
            $"Custo: {slimeCost}. " +
            $"Capacidade: {SlimeCount}/{MaxSlimeCapacity}.",
            this
        );

        return true;
    }

    private bool TryUpgradeHabitat()
    {
        if (manaSystem == null || !CanUpgrade)
        {
            return false;
        }

        if (!manaSystem.TrySpendMana(levelTwoUpgradeCost))
        {
            Debug.Log(
                $"São necessárias {levelTwoUpgradeCost} manas " +
                "para melhorar o habitat.",
                this
            );

            return false;
        }

        habitatLevel = 2;

        UpdatePresentation();
        HabitatUpgraded?.Invoke(habitatLevel);

        Debug.Log(
            $"Habitat melhorado para o nível {habitatLevel}! " +
            $"Capacidade: {MaxSlimeCapacity}. " +
            $"Produção: x{ProductionMultiplier}.",
            this
        );

        return true;
    }

    private long CalculateSlimeCost(int ownedSlimeCount)
    {
        long cost = firstSlimeCost;

        for (int i = 0; i < ownedSlimeCount; i++)
        {
            if (cost > long.MaxValue / 2)
            {
                return long.MaxValue;
            }

            cost *= 2;
        }

        return cost;
    }

    private static SlimeRarity RollSlimeRarity()
    {
        int roll = UnityEngine.Random.Range(0, 100);

        if (roll < 85)
        {
            return SlimeRarity.Common;
        }

        if (roll < 95)
        {
            return SlimeRarity.Rare;
        }

        return SlimeRarity.Legendary;
    }

    private void UpdatePresentation()
    {
        bool isRestored = habitatState != HabitatState.Ruined;

        if (ruinedVisual != null)
        {
            ruinedVisual.SetActive(!isRestored);
        }

        if (restoredVisual != null)
        {
            restoredVisual.SetActive(isRestored);
        }

        UpdateSlimeVisuals();
        UpdatePrompt();
    }

    private void UpdateSlimeVisuals()
    {
        if (slimeVisualSlots == null)
        {
            return;
        }

        for (int i = 0; i < slimeVisualSlots.Length; i++)
        {
            SlimeVisualSlot visualSlot = slimeVisualSlots[i];

            if (visualSlot == null)
            {
                continue;
            }

            if (i < slimes.Count)
            {
                visualSlot.Show(slimes[i]);
            }
            else
            {
                visualSlot.Hide();
            }
        }
    }
    private void UpdatePrompt()
    {
        if (interactionPromptText == null)
        {
            return;
        }

        switch (habitatState)
        {
            case HabitatState.Ruined:
                interactionPromptText.text =
                    $"RESTAURAR\n{restorationCost:N0} MANA";
                break;

            case HabitatState.RestoredEmpty:
            case HabitatState.Occupied:
                if (CanUpgrade)
                {
                    interactionPromptText.text =
                        $"MELHORAR HABITAT\n" +
                        $"{levelTwoUpgradeCost:N0} MANA\n" +
                        $"NÍVEL 1 → 2";
                }
                else if (HasAvailableSlimeSlot)
                {
                    interactionPromptText.text =
                        $"COMPRAR SLIME\n" +
                        $"{NextSlimeCost:N0} MANA\n" +
                        $"SLIMES {SlimeCount}/{MaxSlimeCapacity}";
                }
                else
                {
                    interactionPromptText.text =
                        $"HABITAT NÍVEL {HabitatLevel}\n" +
                        $"SLIMES {SlimeCount}/{MaxSlimeCapacity}";
                }

                break;
        }
    }

    private void OnValidate()
    {
        levelOneSlimeCapacity = Mathf.Max(1, levelOneSlimeCapacity);

        levelTwoSlimeCapacity = Mathf.Max(
            levelOneSlimeCapacity + 1,
            levelTwoSlimeCapacity
        );

        levelTwoUpgradeCost = Math.Max(1L, levelTwoUpgradeCost);
        firstSlimeCost = Math.Max(1L, firstSlimeCost);
    }

    private static string GetRarityLabel(SlimeRarity rarity)
    {
        switch (rarity)
        {
            case SlimeRarity.Common:
                return "COMUM";

            case SlimeRarity.Rare:
                return "RARO";

            case SlimeRarity.Legendary:
                return "LENDÁRIO";

            default:
                return "DESCONHECIDO";
        }
    }
}
