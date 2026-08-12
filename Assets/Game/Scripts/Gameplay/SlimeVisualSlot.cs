using UnityEngine;

public sealed class SlimeVisualSlot : MonoBehaviour
{
    [Header("Visuais por raridade")]
    [SerializeField] private GameObject commonVisual;
    [SerializeField] private GameObject rareVisual;
    [SerializeField] private GameObject legendaryVisual;

    public void Show(SlimeRarity rarity)
    {
        if (commonVisual != null)
        {
            commonVisual.SetActive(rarity == SlimeRarity.Common);
        }

        if (rareVisual != null)
        {
            rareVisual.SetActive(rarity == SlimeRarity.Rare);
        }

        if (legendaryVisual != null)
        {
            legendaryVisual.SetActive(rarity == SlimeRarity.Legendary);
        }
    }

    public void Hide()
    {
        if (commonVisual != null)
        {
            commonVisual.SetActive(false);
        }

        if (rareVisual != null)
        {
            rareVisual.SetActive(false);
        }

        if (legendaryVisual != null)
        {
            legendaryVisual.SetActive(false);
        }
    }
}