using System;
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
    [SerializeField] private long firstSlimeCost = 100;

    [Header("Habitat")]
    [SerializeField] private GameObject ruinedVisual;
    [SerializeField] private GameObject restoredVisual;
    [SerializeField] private TMP_Text interactionPromptText;

    [Header("Slimes")]
    [SerializeField] private GameObject commonSlimeVisual;
    [SerializeField] private GameObject rareSlimeVisual;
    [SerializeField] private GameObject legendarySlimeVisual;

    private HabitatState habitatState = HabitatState.Ruined;
    private SlimeRarity currentSlimeRarity;

    public bool IsRestored =>
        habitatState != HabitatState.Ruined;

    public bool HasSlime =>
        habitatState == HabitatState.Occupied;

    public event Action Restored;
    public event Action<SlimeRarity> SlimePurchased;

    private void Awake()
    {
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
                TryBuyFirstSlime();
                break;

            case HabitatState.Occupied:
                // Futuramente abrirá o painel do habitat.
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

    private bool TryBuyFirstSlime()
    {
        if (manaSystem == null)
        {
            return false;
        }

        if (!manaSystem.TrySpendMana(firstSlimeCost))
        {
            Debug.Log(
                $"São necessárias {firstSlimeCost} manas para comprar o slime.",
                this
            );

            return false;
        }

        currentSlimeRarity = RollSlimeRarity();
        habitatState = HabitatState.Occupied;

        UpdatePresentation();
        SlimePurchased?.Invoke(currentSlimeRarity);

        Debug.Log(
            $"Slime obtido: {GetRarityLabel(currentSlimeRarity)}!",
            this
        );

        return true;
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
        bool isOccupied = habitatState == HabitatState.Occupied;

        if (ruinedVisual != null)
        {
            ruinedVisual.SetActive(!isRestored);
        }

        if (restoredVisual != null)
        {
            restoredVisual.SetActive(isRestored);
        }

        if (commonSlimeVisual != null)
        {
            commonSlimeVisual.SetActive(
                isOccupied &&
                currentSlimeRarity == SlimeRarity.Common
            );
        }

        if (rareSlimeVisual != null)
        {
            rareSlimeVisual.SetActive(
                isOccupied &&
                currentSlimeRarity == SlimeRarity.Rare
            );
        }

        if (legendarySlimeVisual != null)
        {
            legendarySlimeVisual.SetActive(
                isOccupied &&
                currentSlimeRarity == SlimeRarity.Legendary
            );
        }

        UpdatePrompt();
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
                interactionPromptText.text =
                    $"COMPRAR SLIME\n{firstSlimeCost:N0} MANA";
                break;

            case HabitatState.Occupied:
                interactionPromptText.text =
                    $"SLIME\n{GetRarityLabel(currentSlimeRarity)}";
                break;
        }
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