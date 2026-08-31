using System;
using UnityEngine;

public sealed class ManaSystem : MonoBehaviour
{
    [Header("Configuração inicial")]
    [SerializeField] private long startingMana = 0;

    private long currentMana;

    public long CurrentMana => currentMana;

    public event Action<long> ManaChanged;

    private void Awake()
    {
        currentMana = Math.Max(0L, startingMana);
    }

    private void Start()
    {
        NotifyManaChanged();
    }

    public void AddMana(long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMana += amount;
        NotifyManaChanged();
    }

    public bool TrySpendMana(long amount)
    {
        if (amount <= 0 || currentMana < amount)
        {
            return false;
        }

        currentMana -= amount;
        NotifyManaChanged();

        return true;
    }

    public void SetMana(long amount)
    {
        currentMana = Math.Max(0L, amount);
        NotifyManaChanged();
    }
    private void NotifyManaChanged()
    {
        ManaChanged?.Invoke(currentMana);
    }

    [ContextMenu("Teste/Adicionar 10 de Mana")]
    private void AddTestMana()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Entre no Play Mode para testar a mana.");
            return;
        }

        AddMana(10);
    }
}