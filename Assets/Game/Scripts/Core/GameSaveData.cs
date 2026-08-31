using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public long mana;
    public bool grassHabitatRestored;
    public int grassHabitatLevel;
    public List<SlimeRarity> grassSlimes;
    public long lastSaveUnixTime;
}