using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class FirstRunTutorial : MonoBehaviour
{
    internal enum Step { Welcome, TapTotem, CollectRestorationMana, OpenHabitat, RestoreHabitat, CollectSlimeMana, BuySlime, Finish, Complete }
    internal const string ProgressKey = "SlimeVille.Tutorial.Progress.v1";
    private const string TargetKey = "SlimeVille.Tutorial.Habitat.v1";

    [Header("Jogo existente — sem mudar custos ou recursos")]
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private PrimordialTree totem;
    [SerializeField] private SlimeHabitat[] habitats;
    [SerializeField] private HabitatPopupUI habitatPopup;
    [SerializeField] private TutorialPopupView view;

    private Collider totemCollider;
    private Collider[] habitatColliders;
    private Step step;
    private int targetIndex;
    private bool initialized, started, running, subscribed;
    private bool lastPopupOpen;
    private SlimeHabitat lastPopupTarget;

    private void Awake()
    {
        if (manaSystem == null || totem == null || habitatPopup == null || view == null || habitats == null || habitats.Length == 0)
        {
            Debug.LogError("Configure as referências do tutorial inicial.", this);
            enabled = false;
            return;
        }
        habitatColliders = new Collider[habitats.Length];
        for (int i = 0; i < habitats.Length; i++)
        {
            if (habitats[i] == null) { enabled = false; return; }
            habitatColliders[i] = habitats[i].GetComponent<Collider>();
        }
        totemCollider = totem.GetComponent<Collider>();
        view.ContinuePressed += HandleContinue;
        view.SkipPressed += Complete;
        initialized = true;
    }

    private IEnumerator Start()
    {
        // SaveSystem loads synchronously in Start; wait for every Start before inspecting progress.
        yield return null;
        started = true;
        Begin();
    }

    private void OnEnable() { if (started) Begin(); }

    private void Begin()
    {
        if (!initialized || !isActiveAndEnabled || !view.IsConfigured) return;
        StopGuidance();
        if (!PlayerPrefs.HasKey(ProgressKey) && HasExistingHabitatProgress())
        {
            Complete(); // Existing players must not receive a forced introduction after this update.
            return;
        }
        step = (Step)Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, 0), 0, (int)Step.Complete);
        if (step == Step.Complete) { enabled = false; return; }
        targetIndex = Mathf.Clamp(PlayerPrefs.GetInt(TargetKey, DefaultTarget()), 0, habitats.Length - 1);
        running = true;
        Subscribe();
        PlayerPrefs.SetInt(ProgressKey, (int)step);
        PlayerPrefs.Save();
        Refresh();
    }

    private bool HasExistingHabitatProgress()
    {
        foreach (SlimeHabitat habitat in habitats)
            if (habitat.IsRestored || habitat.SlimeCount > 0) return true;
        return false;
    }

    private int DefaultTarget()
    {
        int best = 0;
        for (int i = 0; i < habitats.Length; i++)
        {
            if (habitats[i].IsRestored && habitats[i].SlimeCount == 0) return i;
            if (habitats[i].RestorationCost < habitats[best].RestorationCost) best = i;
        }
        return best;
    }

    private void Subscribe()
    {
        if (subscribed) return;
        manaSystem.ManaChanged += HandleManaChanged;
        totem.ManaTapped += HandleTotemTap;
        foreach (SlimeHabitat habitat in habitats)
        {
            habitat.Restored += Refresh;
            habitat.SlimePurchased += HandleSlimePurchased;
            habitat.ProgressLoaded += Refresh;
        }
        subscribed = true;
    }

    private void StopGuidance()
    {
        if (subscribed)
        {
            if (manaSystem != null) manaSystem.ManaChanged -= HandleManaChanged;
            if (totem != null) totem.ManaTapped -= HandleTotemTap;
            foreach (SlimeHabitat habitat in habitats)
                if (habitat != null)
                {
                    habitat.Restored -= Refresh;
                    habitat.SlimePurchased -= HandleSlimePurchased;
                    habitat.ProgressLoaded -= Refresh;
                }
        }
        subscribed = false;
        running = false;
        if (view != null) view.Hide();
    }

    private void Update()
    {
        if (!running) return;
        if (lastPopupOpen != habitatPopup.IsOpen || lastPopupTarget != habitatPopup.SelectedHabitat) Refresh();
    }

    private void HandleManaChanged(long value) => Refresh();
    private void HandleSlimePurchased(SlimeRarity rarity) => Refresh();
    private void HandleTotemTap(int amount)
    {
        if (!running || amount <= 0) return;
        if (step == Step.Welcome || step == Step.TapTotem) SetStep(Step.CollectRestorationMana);
        Refresh();
    }

    private void Refresh()
    {
        if (!running) return;
        lastPopupOpen = habitatPopup.IsOpen;
        lastPopupTarget = habitatPopup.SelectedHabitat;
        bool anySlime = false;
        for (int i = 0; i < habitats.Length; i++)
        {
            if (habitats[i].SlimeCount > 0) anySlime = true;
            if (lastPopupOpen && habitats[i] == lastPopupTarget) SetTarget(i);
        }
        if (!lastPopupOpen && !habitats[targetIndex].IsRestored)
            for (int i = 0; i < habitats.Length; i++)
                if (habitats[i].IsRestored && habitats[i].SlimeCount == 0) { SetTarget(i); break; }

        SlimeHabitat target = habitats[targetIndex];
        SetStep(ResolveStep(step, manaSystem.CurrentMana, anySlime, target.IsRestored,
            target.RestorationCost, target.NextSlimeCost, lastPopupOpen && lastPopupTarget == target));
        view.AvoidWindow(lastPopupOpen ? habitatPopup.WindowRect : null);
        Render(target);
    }

    internal static Step ResolveStep(Step current, long mana, bool anySlime, bool restored,
        long restorationCost, long slimeCost, bool correctPopupOpen)
    {
        if (current == Step.Complete) return current;
        if (current == Step.Welcome || current == Step.TapTotem || current == Step.Finish) return current;
        if (anySlime) return Step.Finish;
        if (!restored) return mana < restorationCost ? Step.CollectRestorationMana
            : correctPopupOpen ? Step.RestoreHabitat : Step.OpenHabitat;
        return mana < slimeCost ? Step.CollectSlimeMana : correctPopupOpen ? Step.BuySlime : Step.OpenHabitat;
    }

    private void SetTarget(int index)
    {
        if (targetIndex == index) return;
        targetIndex = index;
        PlayerPrefs.SetInt(TargetKey, index);
        PlayerPrefs.Save();
    }

    private void SetStep(Step next)
    {
        if (step == next) return;
        step = next;
        PlayerPrefs.SetInt(ProgressKey, (int)step);
        PlayerPrefs.Save();
    }

    private void Render(SlimeHabitat target)
    {
        view.ClearTarget();
        switch (step)
        {
            case Step.Welcome:
                view.Show("BEM-VINDO", "Restaure a vida da vila",
                    "Recupere os habitats e traga os slimes de volta. A mana é a energia para fazer a vila florescer!", "COMEÇAR");
                break;
            case Step.TapTotem:
                view.Show("1/3  •  MANA", "Tudo começa no totem",
                    "Toque no totem destacado para ganhar mana.");
                view.FocusWorld(totemCollider);
                break;
            case Step.CollectRestorationMana:
            case Step.CollectSlimeMana:
                bool restoring = step == Step.CollectRestorationMana;
                long cost = restoring ? target.RestorationCost : target.NextSlimeCost;
                view.Show(restoring ? "2/3  •  HABITATS" : "3/3  •  SLIMES", "Junte um pouco de mana",
                    (lastPopupOpen ? "Feche a janela e toque no totem." : "Toque no totem para juntar mana.") +
                    $"\n{manaSystem.CurrentMana:N0}/{cost:N0} MANA para " + (restoring ? "restaurar." : "o primeiro slime."),
                    lastPopupOpen ? "VOLTAR AO TOTEM" : null);
                if (lastPopupOpen) view.FocusUI(habitatPopup.CloseButtonRect);
                else view.FocusWorld(totemCollider);
                break;
            case Step.OpenHabitat:
                view.Show(target.IsRestored ? "3/3  •  SLIMES" : "2/3  •  HABITATS", "Toque no habitat",
                    target.IsRestored ? "Abra o habitat destacado para atrair seu primeiro slime."
                        : "Você já tem mana suficiente! Abra o habitat destacado para ver suas ações.");
                view.FocusWorld(habitatColliders[targetIndex]);
                break;
            case Step.RestoreHabitat:
                view.Show("2/3  •  HABITATS", "Devolva a vida ao habitat",
                    $"Toque em RESTAURAR.\nCusto: {target.RestorationCost:N0} mana.");
                view.FocusUI(habitatPopup.RestoreButtonRect);
                break;
            case Step.BuySlime:
                view.Show("3/3  •  SLIMES", "Seu primeiro companheiro",
                    $"Toque em COMPRAR SLIME.\nCusto: {target.NextSlimeCost:N0} mana.");
                view.FocusUI(habitatPopup.BuyButtonRect);
                break;
            case Step.Finish:
                view.Show("PRONTO!", "A vida está voltando!",
                    "Seu slime gera mana automaticamente. Continue restaurando habitats e trazendo novos companheiros para a vila.", "JOGAR");
                break;
        }
    }

    private void HandleContinue()
    {
        if (!running) return;
        if (step == Step.Welcome) { SetStep(Step.TapTotem); Refresh(); }
        else if (step == Step.Finish) Complete();
        else if ((step == Step.CollectRestorationMana || step == Step.CollectSlimeMana) && habitatPopup.IsOpen)
            habitatPopup.Close();
    }

    private void Complete()
    {
        PlayerPrefs.SetInt(ProgressKey, (int)Step.Complete);
        PlayerPrefs.Save();
        step = Step.Complete;
        StopGuidance();
        enabled = false;
    }

    private void OnDisable() => StopGuidance();
    private void OnDestroy()
    {
        StopGuidance();
        if (view != null) { view.ContinuePressed -= HandleContinue; view.SkipPressed -= Complete; }
    }

    [ContextMenu("Tutorial/Reiniciar tutorial para teste")]
    private void RestartForTesting()
    {
        PlayerPrefs.SetInt(ProgressKey, (int)Step.Welcome);
        PlayerPrefs.SetInt(TargetKey, 0);
        PlayerPrefs.Save();
        if (Application.isPlaying)
        {
            if (!enabled) enabled = true;
            else if (started) Begin();
        }
        Debug.Log("Tutorial reiniciado. O progresso do jogo não foi apagado; etapas já realizadas serão reconhecidas.", this);
    }
}
