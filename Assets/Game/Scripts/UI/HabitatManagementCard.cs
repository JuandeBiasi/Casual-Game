using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HabitatManagementCard : MonoBehaviour
{
    [SerializeField] private string habitatName;
    [SerializeField] private SlimeHabitat habitat;
    [SerializeField] private SlimeManaProducer manaProducer;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Image accentImage;

    private Action<HabitatManagementCard> action;

    public SlimeHabitat Habitat => habitat;
    public SlimeManaProducer ManaProducer => manaProducer;
    public string DisplayName => habitatName;

    public void Initialize(Action<HabitatManagementCard> onAction)
    {
        action = onAction;
        if (actionButton == null) return;
        actionButton.onClick.RemoveListener(HandleAction);
        actionButton.onClick.AddListener(HandleAction);
    }

    public void Refresh(bool slimeTab, long currentMana)
    {
        if (titleText != null)
        {
            titleText.text = habitatName;
            if (accentImage != null) titleText.color = accentImage.color;
        }
        if (habitat == null || manaProducer == null)
        {
            SetAction("Indisponível", false, "Configure as referências deste habitat.");
            return;
        }

        int common = 0, rare = 0, legendary = 0;
        foreach (SlimeRarity rarity in habitat.Slimes)
        {
            if (rarity == SlimeRarity.Common) common++;
            else if (rarity == SlimeRarity.Rare) rare++;
            else if (rarity == SlimeRarity.Legendary) legendary++;
        }

        if (detailsText != null)
        {
            string state = habitat.IsRestored
                ? $"Nível {habitat.HabitatLevel}"
                : "Não restaurado";
            detailsText.text = $"{state}  •  {habitat.SlimeCount}/{habitat.MaxSlimeCapacity} slimes\n" +
                (slimeTab
                    ? "85% comum  •  10% raro  •  5% lendário"
                    : $"Comuns: {common}  •  Raros: {rare}  •  Lendários: {legendary}");
        }

        if (slimeTab)
        {
            if (!habitat.IsRestored)
                SetAction("Comprar slime", false, "Restaure primeiro na aba Habitats.");
            else if (!habitat.HasAvailableSlimeSlot)
                SetAction("Habitat lotado", false, habitat.HabitatLevel == 1
                    ? "Aumente a capacidade na aba Habitats."
                    : "Todos os espaços estão ocupados.");
            else
                SetPricedAction("Comprar slime", habitat.NextSlimeCost, currentMana,
                    "O novo slime pertence a este habitat.");
            return;
        }

        if (!habitat.IsRestored)
            SetPricedAction("Restaurar", habitat.RestorationCost, currentMana,
                "Restaura o habitat vazio, com 4 espaços.");
        else if (habitat.HabitatLevel >= 2)
            SetAction("Nível máximo", false, "Este habitat já está no nível 2.");
        else if (!habitat.CanUpgrade)
            SetAction($"Melhorar • {habitat.UpgradeCost:N0} mana", false,
                $"Tenha {habitat.LevelOneSlimeCapacity} slimes para liberar a melhoria.");
        else
            SetPricedAction("Melhorar", habitat.UpgradeCost, currentMana,
                "Nível 2: 6 espaços e produção dobrada.");
    }

    private void SetPricedAction(string label, long cost, long currentMana, string hint)
    {
        bool affordable = currentMana >= cost;
        SetAction($"{label} • {cost:N0} mana", affordable,
            affordable ? hint : $"Faltam {cost - currentMana:N0} mana.");
    }

    private void SetAction(string label, bool enabled, string hint)
    {
        if (actionText != null) actionText.text = label;
        if (actionButton != null) actionButton.interactable = enabled;
        if (hintText != null) hintText.text = hint;
    }

    private void HandleAction() => action?.Invoke(this);

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(HandleAction);
    }
}
