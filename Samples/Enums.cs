namespace Hexborne.Gameplay
{
    public enum Element { Fire, Ice, Shadow }

    public enum SkillForm { Projectile, Area, Aura }

    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    /// <summary>Player-wide stats changed by generic cards. Percent stats are stored as fractions (0.05 = +5%).</summary>
    public enum StatType
    {
        Damage,
        CastSpeed,
        Area,
        Multicast,
        CritChance,
        CritDamage,
        MaxHealth,
        MoveSpeed,
        MagnetRadius,
        XpGain,
        /// <summary>Flat: every hit on the player is this much smaller (never below PlayerConfig.MinDamageTaken).</summary>
        Armor,
        /// <summary>Flat: health per second.</summary>
        Regeneration
    }

    public enum StatusType { None, Burn, Frost, Doom }

    public enum ReactionType { Steam, Shatter, Soulfire }

    public enum LegendaryType { MirrorMage, Starfall, EternalWinter, SoulReaper, ChainReaction }

    /// <summary>Per-skill or per-tag stat. Percent values are fractions; Count is a whole number of extra projectiles.</summary>
    public enum SkillStat { Damage, Frequency, Area, Duration, Multicast, Count }

    public enum PowerKind
    {
        /// <summary>Player-wide stat modifiers (Power, Haste, Vitality...).</summary>
        Stat,
        /// <summary>Grants a new active skill.</summary>
        NewSkill,
        /// <summary>Improves one stat of one equipped skill.</summary>
        SkillUpgrade,
        /// <summary>Improves one stat of every skill with an element.</summary>
        ElementUpgrade,
        /// <summary>Improves one stat of every skill with a form.</summary>
        FormUpgrade,
        /// <summary>All hits (or crits) may apply a status.</summary>
        StatusProc,
        /// <summary>Strengthens a status for the whole build.</summary>
        StatusBoost,
        /// <summary>Applying one status may also apply another status (depth 1).</summary>
        StatusRing,
        /// <summary>Unlocks a reaction between statuses for the whole build.</summary>
        Resonance,
        /// <summary>Unlocks an element's charge and auto-ultimate.</summary>
        Heart,
        /// <summary>After a primary cast of one form, may cast an equipped skill of another form (Echo).</summary>
        SkillChain,
        /// <summary>Unlocks a legendary rule for the whole build.</summary>
        Legendary
    }

    public enum ProcTrigger { OnHit, OnCrit }

    public enum StatusBoostType
    {
        BurnDamage,
        BurnTickRate,
        FrostThresholdReduction,
        FreezeDuration,
        DoomExecuteThresholdReduction,
        DoomSplashRadius
    }

    public enum ConditionType
    {
        HasFreeSlot,
        SkillNotTaken,
        HasSkill,
        MinSkillsWithElement,
        MinSkillsWithForm,
        MinStatusSources,
        HasHeart,
        MinReactions
    }

    public enum EnemyBehaviour { Chase, Ranged, Bomber }
}
