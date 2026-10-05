using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class HabitatManagementMenu : MonoBehaviour
{
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button habitatsTabButton;
    [SerializeField] private Button slimesTabButton;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text sectionDescription;
    [SerializeField] private Image habitatsTabBackground;
    [SerializeField] private Image slimesTabBackground;
    [SerializeField] private HabitatManagementCard[] cards;

    private bool slimeTab;
    private bool started;
    private bool subscribed;
    private static readonly Color SelectedTab = new Color(0.24f, 0.43f, 0.57f, 1f);
    private static readonly Color OtherTab = new Color(0.18f, 0.23f, 0.30f, 1f);

    private void Start()
    {
        if (manaSystem == null || panelRoot == null || cards == null)
        {
            Debug.LogError("Configure as referências do menu de habitats.", this);
            enabled = false;
            return;
        }

        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (habitatsTabButton != null) habitatsTabButton.onClick.AddListener(ShowHabitats);
        if (slimesTabButton != null) slimesTabButton.onClick.AddListener(ShowSlimes);
        foreach (HabitatManagementCard card in cards)
            if (card != null) card.Initialize(HandleCardAction);

        started = true;
        panelRoot.SetActive(false);
        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        if (!started) return;
        Subscribe();
        Refresh();
    }

    private void OnDisable() => Unsubscribe();

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        manaSystem.ManaChanged += HandleManaChanged;
        foreach (HabitatManagementCard card in cards)
        {
            if (card == null) continue;
            if (card.Habitat != null)
            {
                card.Habitat.Restored += Refresh;
                card.Habitat.SlimePurchased += HandleSlimePurchased;
                card.Habitat.HabitatUpgraded += HandleHabitatUpgraded;
                card.Habitat.ProgressLoaded += Refresh;
            }
            if (card.ManaProducer != null)
                card.ManaProducer.ProductionChanged += HandleProductionChanged;
        }
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (manaSystem != null) manaSystem.ManaChanged -= HandleManaChanged;
        foreach (HabitatManagementCard card in cards)
        {
            if (card == null) continue;
            if (card.Habitat != null)
            {
                card.Habitat.Restored -= Refresh;
                card.Habitat.SlimePurchased -= HandleSlimePurchased;
                card.Habitat.HabitatUpgraded -= HandleHabitatUpgraded;
                card.Habitat.ProgressLoaded -= Refresh;
            }
            if (card.ManaProducer != null)
                card.ManaProducer.ProductionChanged -= HandleProductionChanged;
        }
        subscribed = false;
    }

    private void Open()
    {
        panelRoot.SetActive(true);
        SetStatus("");
        Refresh();
    }

    private void Close() => panelRoot.SetActive(false);
    private void ShowHabitats() => SetTab(false);
    private void ShowSlimes() => SetTab(true);

    private void SetTab(bool showSlimes)
    {
        slimeTab = showSlimes;
        SetStatus("");
        Refresh();
    }

    private void Refresh()
    {
        if (!started || manaSystem == null) return;
        if (manaText != null) manaText.text = $"{manaSystem.CurrentMana:N0} MANA";
        if (sectionDescription != null)
            sectionDescription.text = slimeTab
                ? "Compre slimes para o habitat escolhido."
                : "Restaure, melhore e acompanhe cada habitat.";
        if (habitatsTabBackground != null)
            habitatsTabBackground.color = slimeTab ? OtherTab : SelectedTab;
        if (slimesTabBackground != null)
            slimesTabBackground.color = slimeTab ? SelectedTab : OtherTab;
        foreach (HabitatManagementCard card in cards)
            if (card != null) card.Refresh(slimeTab, manaSystem.CurrentMana);
    }

    private void HandleCardAction(HabitatManagementCard card)
    {
        SlimeHabitat habitat = card.Habitat;
        if (habitat == null) return;
        bool success;
        string message;
        if (slimeTab)
        {
            success = habitat.TryBuySlime();
            string rarity = success && habitat.SlimeCount > 0
                ? GetRarityLabel(habitat.Slimes[habitat.SlimeCount - 1])
                : "";
            message = $"{card.DisplayName}: slime {rarity} comprado!";
        }
        else if (!habitat.IsRestored)
        {
            success = habitat.TryRestore();
            message = $"{card.DisplayName} restaurado!";
        }
        else
        {
            success = habitat.TryUpgradeHabitat();
            message = $"{card.DisplayName}: nível {habitat.HabitatLevel}!";
        }
        SetStatus(success ? message : "Ação indisponível. Confira a mana e os requisitos.");
        Refresh();
    }

    private static string GetRarityLabel(SlimeRarity rarity)
    {
        switch (rarity)
        {
            case SlimeRarity.Rare: return "raro";
            case SlimeRarity.Legendary: return "lendário";
            default: return "comum";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void HandleManaChanged(long amount) => Refresh();
    private void HandleSlimePurchased(SlimeRarity rarity) => Refresh();
    private void HandleHabitatUpgraded(int level) => Refresh();
    private void HandleProductionChanged(long amount) => Refresh();

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (habitatsTabButton != null) habitatsTabButton.onClick.RemoveListener(ShowHabitats);
        if (slimesTabButton != null) slimesTabButton.onClick.RemoveListener(ShowSlimes);
    }
}
