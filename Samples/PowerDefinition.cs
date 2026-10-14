using UnityEngine;

namespace Hexborne.Gameplay
{
    [System.Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public float Value;
    }

    /// <summary>A requirement the build must meet for a card to appear. Only the fields of its Type are read.</summary>
    [System.Serializable]
    public struct PowerCondition
    {
        public ConditionType Type;
        public SkillDefinition Skill;
        public Element Element;
        public SkillForm Form;
        public StatusType Status;
        public int Count;
    }

    /// <summary>
    /// A level-up or chest card. <see cref="Kind"/> decides which fields are used (docs/CONCEPT.md, "Карта карточек"):
    /// Stat → Modifiers; NewSkill → Skill; SkillUpgrade → Skill + SkillStat/Value; ElementUpgrade → Element + SkillStat/Value;
    /// FormUpgrade → Form + SkillStat/Value; StatusProc → Status, Trigger, ProcChance, ProcStacks; StatusBoost → Boost/Value.
    /// StatusRing → Status (trigger), RingTarget, ProcChance, ProcStacks; Resonance → Reaction; Heart → Element.
    /// SkillChain → Form (source), ChainTarget, ProcChance; Legendary → Legendary.
    /// </summary>
    [CreateAssetMenu(menuName = "Hexborne/Power", fileName = "Power")]
    public sealed class PowerDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Rarity Rarity;
        public PowerKind Kind;
        [Tooltip("Can be picked once per run.")]
        public bool Unique;
        public PowerCondition[] Conditions = new PowerCondition[0];

        [Header("Stat")]
        public StatModifier[] Modifiers = new StatModifier[0];

        [Header("Skill / tag")]
        public SkillDefinition Skill;
        public Element Element;
        public SkillForm Form;
        [Tooltip("SkillChain: target form; Form is the source.")] public SkillForm ChainTarget;
        public SkillStat SkillStat;
        public float Value;

        [Header("Status")]
        public StatusType Status;
        [Tooltip("StatusRing: status applied when Status lands.")] public StatusType RingTarget;
        public ProcTrigger Trigger;
        [Range(0f, 1f)] public float ProcChance;
        public int ProcStacks = 1;
        public StatusBoostType Boost;
        [Tooltip("Resonance: reaction unlocked by this card.")] public ReactionType Reaction;
        [Tooltip("Legendary: rule unlocked by this card.")] public LegendaryType Legendary;
    }
}
