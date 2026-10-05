using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SaveSystem : MonoBehaviour
{
    [Header("Mana")]
    [SerializeField] private ManaSystem manaSystem;

    [Header("Habitat de grama")]
    [SerializeField] private SlimeHabitat grassHabitat;
    [SerializeField] private SlimeManaProducer slimeManaProducer;

    [Header("Habitat de fogo")]
    [SerializeField] private SlimeHabitat fireHabitat;
    [SerializeField] private SlimeManaProducer fireManaProducer;

    [Header("Habitat de água")]
    [SerializeField] private SlimeHabitat waterHabitat;
    [SerializeField] private SlimeManaProducer waterManaProducer;

    [Header("Salvamento automático")]
    [SerializeField, Min(1f)] private float autosaveInterval = 5f;

    [Header("Produção offline")]
    [SerializeField, Min(1)] private int maxOfflineHours = 4;

    private bool isInitialized;
    private bool wasPaused;

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "save.json");

    private void Start()
    {
        if (!ValidateReferences())
        {
            return;
        }

        isInitialized = true;

        LoadGame();

        // Registra imediatamente a mana já acrescida
        // e o novo horário de referência.
        SaveGame();

        StartCoroutine(AutosaveRoutine());
    }

    public void SaveGame()
    {
        if (!isInitialized)
        {
            return;
        }

        GameSaveData saveData = new GameSaveData
        {
            mana = manaSystem.CurrentMana,
            grassHabitatRestored = grassHabitat.IsRestored,
            grassHabitatLevel = grassHabitat.HabitatLevel,

            grassSlimes = new List<SlimeRarity>(
                grassHabitat.Slimes
            ),

            fireHabitatRestored = fireHabitat.IsRestored,
            fireHabitatLevel = fireHabitat.HabitatLevel,
            fireSlimes = new List<SlimeRarity>(
                fireHabitat.Slimes
            ),

            waterHabitatRestored = waterHabitat.IsRestored,
            waterHabitatLevel = waterHabitat.HabitatLevel,
            waterSlimes = new List<SlimeRarity>(
                waterHabitat.Slimes
            ),

            lastSaveUnixTime = GetCurrentUnixTime()
        };

        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Não foi possível salvar o jogo: {exception.Message}",
                this
            );
        }
    }

    private void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("Nenhum save encontrado. Iniciando novo jogo.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            GameSaveData saveData =
                JsonUtility.FromJson<GameSaveData>(json);

            if (saveData == null)
            {
                Debug.LogWarning("O arquivo de save está vazio.");
                return;
            }

            // Primeiro restaura a mana salva.
            manaSystem.SetMana(saveData.mana);

            // Depois restaura slimes, nível e multiplicador de cada habitat.
            grassHabitat.LoadProgress(
                saveData.grassHabitatRestored,
                Mathf.Max(1, saveData.grassHabitatLevel),
                saveData.grassSlimes
            );

            fireHabitat.LoadProgress(
                saveData.fireHabitatRestored,
                Mathf.Max(1, saveData.fireHabitatLevel),
                saveData.fireSlimes
            );

            waterHabitat.LoadProgress(
                saveData.waterHabitatRestored,
                Mathf.Max(1, saveData.waterHabitatLevel),
                saveData.waterSlimes
            );

            // Calcula o ganho depois de carregar os três habitats.
            ApplyOfflineEarnings(saveData.lastSaveUnixTime);

            Debug.Log("Progresso carregado com sucesso.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Não foi possível carregar o jogo: {exception.Message}",
                this
            );
        }
    }

    private void ApplyOfflineEarnings(long savedUnixTime)
    {
        if (savedUnixTime <= 0)
        {
            return;
        }

        long currentUnixTime = GetCurrentUnixTime();

        long elapsedSeconds = Math.Max(
            0L,
            currentUnixTime - savedUnixTime
        );

        long maximumOfflineSeconds =
            maxOfflineHours * 60L * 60L;

        long consideredSeconds = Math.Min(
            elapsedSeconds,
            maximumOfflineSeconds
        );

        long manaPerSecond =
            slimeManaProducer.TotalManaPerSecond +
            fireManaProducer.TotalManaPerSecond +
            waterManaProducer.TotalManaPerSecond;

        long offlineMana =
            consideredSeconds * manaPerSecond;

        if (offlineMana <= 0)
        {
            return;
        }

        manaSystem.AddMana(offlineMana);

        Debug.Log(
            $"Ganho offline: +{offlineMana:N0} mana " +
            $"por {consideredSeconds:N0} segundos.",
            this
        );
    }

    private static long GetCurrentUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private IEnumerator AutosaveRoutine()
    {
        WaitForSecondsRealtime wait =
            new WaitForSecondsRealtime(autosaveInterval);

        while (true)
        {
            yield return wait;
            SaveGame();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isInitialized)
        {
            return;
        }

        if (isPaused)
        {
            SaveGame();
            wasPaused = true;
        }
        else if (wasPaused)
        {
            // O Android pode manter o processo vivo enquanto
            // o aplicativo está em segundo plano.
            LoadGame();
            SaveGame();

            wasPaused = false;
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private bool ValidateReferences()
    {
        if (manaSystem == null ||
            grassHabitat == null ||
            slimeManaProducer == null ||
            fireHabitat == null ||
            fireManaProducer == null ||
            waterHabitat == null ||
            waterManaProducer == null)
        {
            Debug.LogError(
                "As referências do SaveSystem não foram configuradas.",
                this
            );

            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        autosaveInterval = Mathf.Max(1f, autosaveInterval);
        maxOfflineHours = Mathf.Max(1, maxOfflineHours);
    }

    [ContextMenu("Save/Apagar progresso salvo")]
    private void DeleteSave()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning(
                "Saia do Play Mode antes de apagar o save.",
                this
            );

            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.Log("Nenhum arquivo de save foi encontrado.", this);
            return;
        }

        File.Delete(SavePath);

        Debug.Log(
            "Progresso salvo apagado com sucesso.",
            this
        );
    }
}
