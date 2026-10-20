using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class HabitatPopupUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject optionsPanelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button habitatActionButton;
    [SerializeField] private Button buySlimeButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text habitatActionText;
    [SerializeField] private TMP_Text habitatHintText;
    [SerializeField] private TMP_Text buySlimeText;
    [SerializeField] private TMP_Text buyHintText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image accentImage;

    [Header("Janela próxima ao habitat")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform window;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector2 windowSize = new Vector2(440f, 530f);
    [SerializeField] private Vector2 edgePadding = new Vector2(20f, 20f);
    [SerializeField, Min(0f)] private float habitatGap = 18f;
    [SerializeField, Min(0f)] private float topHudPadding = 136f;

    private SlimeHabitat selectedHabitat;
    private Collider selectedCollider;
    private RectTransform windowParent;
    private int openedFrame;
    private bool initialized;
    private bool subscribed;

    private void Awake()
    {
        if (manaSystem == null || panelRoot == null || closeButton == null ||
            habitatActionButton == null || buySlimeButton == null || targetCanvas == null || window == null)
        {
            Debug.LogError("Configure as referências do popup de habitat.", this);
            enabled = false;
            return;
        }
        windowParent = window.parent as RectTransform;
        if (worldCamera == null) worldCamera = Camera.main;
        if (windowParent == null || worldCamera == null)
        {
            Debug.LogError("Configure a câmera e o RectTransform da janela do habitat.", this);
            enabled = false;
            return;
        }
        closeButton.onClick.AddListener(Close);
        habitatActionButton.onClick.AddListener(HandleHabitatAction);
        buySlimeButton.onClick.AddListener(HandleBuySlime);
        initialized = true;
        panelRoot.SetActive(false);
    }

    public void Open(SlimeHabitat habitat, string habitatName, Color accent)
    {
        if (!initialized || !isActiveAndEnabled || habitat == null || !habitat.isActiveAndEnabled) return;
        Unsubscribe();
        selectedHabitat = habitat;
        selectedCollider = habitat.GetComponent<Collider>();
        openedFrame = Time.frameCount;
        if (titleText != null) titleText.text = habitatName;
        if (accentImage != null) accentImage.color = accent;
        if (optionsPanelRoot != null) optionsPanelRoot.SetActive(false);
        SetStatus("");
        panelRoot.SetActive(true);
        Subscribe();
        Refresh();
        Canvas.ForceUpdateCanvases();
        PositionWindow();
    }

    public void Close()
    {
        Unsubscribe();
        selectedHabitat = null;
        selectedCollider = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (!initialized || !panelRoot.activeSelf) return;
        if (selectedHabitat == null || !selectedHabitat.isActiveAndEnabled ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            Close();
            return;
        }
        // The opening click must not immediately dismiss the window.
        if (Time.frameCount != openedFrame && TryGetPointerDown(out Vector2 screenPosition) &&
            !RectTransformUtility.RectangleContainsScreenPoint(window, screenPosition, UiCamera))
            Close();
    }

    private void LateUpdate()
    {
        if (initialized && panelRoot.activeSelf && selectedHabitat != null)
            PositionWindow();
    }

    private Camera UiCamera => targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
        ? null : targetCanvas.worldCamera;

    private void PositionWindow()
    {
        if (worldCamera == null || windowParent == null) return;
        Bounds bounds = selectedCollider != null ? selectedCollider.bounds
            : new Bounds(selectedHabitat.transform.position, Vector3.one * 0.25f);
        if (worldCamera.WorldToScreenPoint(bounds.center).z <= 0f)
        {
            Close();
            return;
        }

        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        if (!ToLocal(safeArea.min, out Vector2 min) || !ToLocal(safeArea.max, out Vector2 max)) return;
        Rect parentRect = windowParent.rect;
        min = Vector2.Max(min, parentRect.min) + edgePadding;
        max = Vector2.Min(max, parentRect.max) - edgePadding - new Vector2(0f, topHudPadding);
        if (max.x <= min.x || max.y <= min.y) return;
        Rect available = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        Vector2 size = new Vector2(Mathf.Max(1f, windowSize.x), Mathf.Max(1f, windowSize.y));
        float scale = Mathf.Min(1f, Mathf.Min(available.width / size.x, available.height / size.y));
        window.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        window.localScale = new Vector3(scale, scale, 1f);

        Vector2 habitatMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 habitatMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        // Project all corners: the clickable area is offset from the habitat root in this scene.
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            if (!ToLocal(worldCamera.WorldToScreenPoint(corner), out Vector2 point)) return;
            habitatMin = Vector2.Min(habitatMin, point);
            habitatMax = Vector2.Max(habitatMax, point);
        }
        Rect habitatRect = Rect.MinMaxRect(habitatMin.x, habitatMin.y, habitatMax.x, habitatMax.y);
        window.localPosition = CalculateWindowPosition(available, habitatRect, size * scale, habitatGap);
    }

    private bool ToLocal(Vector2 screenPoint, out Vector2 localPoint) =>
        RectTransformUtility.ScreenPointToLocalPointInRectangle(windowParent, screenPoint, UiCamera, out localPoint);

    internal static Vector2 CalculateWindowPosition(Rect available, Rect habitat, Vector2 size, float gap)
    {
        Vector2 half = size * 0.5f;
        float right = habitat.xMax + gap + half.x;
        float left = habitat.xMin - gap - half.x;
        float x = right;
        if (right + half.x > available.xMax)
            x = left - half.x >= available.xMin ? left
                : (available.xMax - habitat.xMax >= habitat.xMin - available.xMin ? right : left);
        float y = habitat.yMax + gap * 0.5f + half.y;
        return new Vector2(Mathf.Clamp(x, available.xMin + half.x, available.xMax - half.x),
            Mathf.Clamp(y, available.yMin + half.y, available.yMax - half.y));
    }

    private static bool TryGetPointerDown(out Vector2 position)
    {
        if (Touchscreen.current != null)
            foreach (var touch in Touchscreen.current.touches)
                if (touch.press.wasPressedThisFrame)
                {
                    position = touch.position.ReadValue();
                    return true;
                }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }
        position = default;
        return false;
    }

    private void Subscribe()
    {
        if (subscribed || selectedHabitat == null) return;
        manaSystem.ManaChanged += HandleManaChanged;
        selectedHabitat.Restored += Refresh;
        selectedHabitat.SlimePurchased += HandleSlimePurchased;
        selectedHabitat.HabitatUpgraded += HandleHabitatUpgraded;
        selectedHabitat.ProgressLoaded += Refresh;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (manaSystem != null) manaSystem.ManaChanged -= HandleManaChanged;
        if (selectedHabitat != null)
        {
            selectedHabitat.Restored -= Refresh;
            selectedHabitat.SlimePurchased -= HandleSlimePurchased;
            selectedHabitat.HabitatUpgraded -= HandleHabitatUpgraded;
            selectedHabitat.ProgressLoaded -= Refresh;
        }
        subscribed = false;
    }

    private void Refresh()
    {
        SlimeHabitat habitat = selectedHabitat;
        if (habitat == null) return;
        long mana = manaSystem.CurrentMana;
        if (infoText != null)
            infoText.text = (habitat.IsRestored ? $"Nível {habitat.HabitatLevel}" : "Não restaurado") +
                $"  •  {habitat.SlimeCount}/{habitat.MaxSlimeCapacity} slimes";

        int common = 0, rare = 0, legendary = 0;
        foreach (SlimeRarity rarity in habitat.Slimes)
        {
            if (rarity == SlimeRarity.Common) common++;
            else if (rarity == SlimeRarity.Rare) rare++;
            else if (rarity == SlimeRarity.Legendary) legendary++;
        }
        if (rarityText != null)
            rarityText.text = $"Comuns: {common}  •  Raros: {rare}\nLendários: {legendary}";

        if (!habitat.IsRestored)
            SetHabitatAction($"RESTAURAR • {habitat.RestorationCost:N0} MANA", mana >= habitat.RestorationCost,
                PriceHint(habitat.RestorationCost, mana, "Restaura um habitat vazio."));
        else if (habitat.HabitatLevel >= 2)
            SetHabitatAction("NÍVEL MÁXIMO", false, $"Capacidade máxima: {habitat.MaxSlimeCapacity} slimes.");
        else if (!habitat.CanUpgrade)
            SetHabitatAction($"MELHORAR • {habitat.UpgradeCost:N0} MANA", false,
                $"Requer {habitat.LevelOneSlimeCapacity} slimes neste habitat.");
        else
            SetHabitatAction($"MELHORAR • {habitat.UpgradeCost:N0} MANA", mana >= habitat.UpgradeCost,
                PriceHint(habitat.UpgradeCost, mana, "Mais espaços e produção x2."));

        bool canBuy = habitat.HasAvailableSlimeSlot && mana >= habitat.NextSlimeCost;
        buySlimeButton.interactable = canBuy;
        if (buySlimeText != null)
            buySlimeText.text = !habitat.HasAvailableSlimeSlot && habitat.IsRestored
                ? "HABITAT LOTADO"
                : $"COMPRAR SLIME • {habitat.NextSlimeCost:N0} MANA";
        if (buyHintText != null)
            buyHintText.text = !habitat.IsRestored ? "Restaure o habitat primeiro."
                : !habitat.HasAvailableSlimeSlot ? (habitat.HabitatLevel == 1
                    ? "Melhore o habitat para liberar mais espaços." : "Todos os espaços estão ocupados.")
                : PriceHint(habitat.NextSlimeCost, mana, "85% comum • 10% raro • 5% lendário");
    }

    private void SetHabitatAction(string text, bool available, string hint)
    {
        habitatActionButton.interactable = available;
        if (habitatActionText != null) habitatActionText.text = text;
        if (habitatHintText != null) habitatHintText.text = hint;
    }

    private static string PriceHint(long cost, long mana, string availableHint) =>
        mana >= cost ? availableHint : $"Faltam {cost - mana:N0} mana.";

    private void HandleHabitatAction()
    {
        SlimeHabitat habitat = selectedHabitat;
        if (habitat == null) return;
        bool restoring = !habitat.IsRestored;
        bool success = restoring ? habitat.TryRestore() : habitat.TryUpgradeHabitat();
        SetStatus(success ? (restoring ? "Habitat restaurado!" : "Habitat no nível 2!")
            : "Confira a mana e os requisitos.");
        Refresh();
    }

    private void HandleBuySlime()
    {
        SlimeHabitat habitat = selectedHabitat;
        if (habitat == null) return;
        bool success = habitat.TryBuySlime();
        SetStatus(success ? $"Slime {RarityLabel(habitat.Slimes[habitat.SlimeCount - 1]).ToLowerInvariant()} adquirido!"
            : "Confira a mana e os espaços.");
        Refresh();
    }

    private static string RarityLabel(SlimeRarity rarity) => rarity == SlimeRarity.Rare ? "Raro"
        : rarity == SlimeRarity.Legendary ? "Lendário" : "Comum";
    private void SetStatus(string text) { if (statusText != null) statusText.text = text; }
    private void HandleManaChanged(long value) => Refresh();
    private void HandleSlimePurchased(SlimeRarity value) => Refresh();
    private void HandleHabitatUpgraded(int value) => Refresh();
    private void OnDisable() => Close();
    private void OnDestroy()
    {
        Close();
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (habitatActionButton != null) habitatActionButton.onClick.RemoveListener(HandleHabitatAction);
        if (buySlimeButton != null) buySlimeButton.onClick.RemoveListener(HandleBuySlime);
    }
}
