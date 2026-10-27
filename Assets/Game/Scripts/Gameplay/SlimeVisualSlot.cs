using UnityEngine;

public sealed class SlimeVisualSlot : MonoBehaviour
{
    [Header("Visuais por raridade")]
    [SerializeField] private GameObject commonVisual;
    [SerializeField] private GameObject rareVisual;
    [SerializeField] private GameObject legendaryVisual;

    private SlimeIdleAnimation idleAnimation;
    private bool animationCached;

    private SlimeIdleAnimation IdleAnimation
    {
        get
        {
            if (!animationCached)
            {
                idleAnimation = GetComponent<SlimeIdleAnimation>();
                animationCached = true;
            }
            return idleAnimation;
        }
    }

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

        GameObject visual = rarity == SlimeRarity.Common ? commonVisual
            : rarity == SlimeRarity.Rare ? rareVisual
            : rarity == SlimeRarity.Legendary ? legendaryVisual : null;
        if (IdleAnimation != null) IdleAnimation.Play(visual != null ? visual.transform : null);
    }

    public void Hide()
    {
        if (IdleAnimation != null) IdleAnimation.Stop();

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
