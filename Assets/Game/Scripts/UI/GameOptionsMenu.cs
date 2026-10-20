using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class GameOptionsMenu : MonoBehaviour
{
    private const string MusicMuteKey = "SlimeVille.Audio.MusicMuted";
    private const string SfxMuteKey = "SlimeVille.Audio.SfxMuted";

    [Header("Interface")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private HabitatPopupUI habitatPopup;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button sfxButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text musicStatusText;
    [SerializeField] private TMP_Text sfxStatusText;
    [SerializeField] private Color soundOnColor = new Color(0.47f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color soundOffColor = new Color(0.67f, 0.71f, 0.77f, 1f);

    [Header("Áudio separado por categoria")]
    [SerializeField] private AudioSource[] musicSources;
    [SerializeField] private AudioSource[] sfxSources;

    [Header("Progresso ao sair")]
    [SerializeField] private SaveSystem saveSystem;

    private bool musicMuted;
    private bool sfxMuted;
    private bool initialized;

    private void Awake()
    {
        if (panelRoot == null || openButton == null || closeButton == null ||
            musicButton == null || sfxButton == null || quitButton == null)
        {
            Debug.LogWarning("Configure as referências do menu de opções.", this);
            enabled = false;
            return;
        }

        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
        musicButton.onClick.AddListener(ToggleMusic);
        sfxButton.onClick.AddListener(ToggleSfx);
        quitButton.onClick.AddListener(QuitGame);
        initialized = true;
        panelRoot.SetActive(false);

        musicMuted = PlayerPrefs.GetInt(MusicMuteKey, DefaultMute(musicSources)) == 1;
        sfxMuted = PlayerPrefs.GetInt(SfxMuteKey, DefaultMute(sfxSources)) == 1;
        ApplyAudioSettings();
    }

    private void Update()
    {
        if (initialized && panelRoot.activeSelf && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void Open()
    {
        if (habitatPopup != null) habitatPopup.Close();
        panelRoot.SetActive(true);
        RefreshLabels();
    }

    private void Close() => panelRoot.SetActive(false);

    private void ToggleMusic()
    {
        musicMuted = !musicMuted;
        PlayerPrefs.SetInt(MusicMuteKey, musicMuted ? 1 : 0);
        ApplyAudioSettings();
        PlayerPrefs.Save();
    }

    private void ToggleSfx()
    {
        sfxMuted = !sfxMuted;
        PlayerPrefs.SetInt(SfxMuteKey, sfxMuted ? 1 : 0);
        ApplyAudioSettings();
        PlayerPrefs.Save();
    }

    private void ApplyAudioSettings()
    {
        SetMuted(musicSources, musicMuted);
        SetMuted(sfxSources, sfxMuted);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (musicStatusText != null)
        {
            musicStatusText.text = musicMuted ? "MUTADA" : "LIGADA";
            musicStatusText.color = musicMuted ? soundOffColor : soundOnColor;
        }
        if (sfxStatusText != null)
        {
            sfxStatusText.text = sfxMuted ? "MUTADOS" : "LIGADOS";
            sfxStatusText.color = sfxMuted ? soundOffColor : soundOnColor;
        }
    }

    private static void SetMuted(AudioSource[] sources, bool muted)
    {
        if (sources == null) return;
        foreach (AudioSource source in sources)
            if (source != null) source.mute = muted;
    }

    private static int DefaultMute(AudioSource[] sources)
    {
        if (sources != null)
            foreach (AudioSource source in sources)
                if (source != null) return source.mute ? 1 : 0;
        return 0;
    }

    private void QuitGame()
    {
        if (saveSystem != null) saveSystem.SaveGame();
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        if (initialized && panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (musicButton != null) musicButton.onClick.RemoveListener(ToggleMusic);
        if (sfxButton != null) sfxButton.onClick.RemoveListener(ToggleSfx);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
    }
}
