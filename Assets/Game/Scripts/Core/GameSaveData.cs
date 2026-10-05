using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public long mana;
    public bool grassHabitatRestored;
    public int grassHabitatLevel;
    public List<SlimeRarity> grassSlimes;

    public bool fireHabitatRestored;
    public int fireHabitatLevel;
    public List<SlimeRarity> fireSlimes;

    public bool waterHabitatRestored;
    public int waterHabitatLevel;
    public List<SlimeRarity> waterSlimes;

    public long lastSaveUnixTime;
}
