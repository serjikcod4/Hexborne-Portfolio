using System;
using System.Collections.Generic;
using Zenject;

namespace Hexborne.Gameplay
{
    /// <summary>A status proc picked from a card: hits (or crits) may apply a status.</summary>
    public struct StatusProc
    {
        public StatusType Status;
        public ProcTrigger Trigger;
        public float Chance;
        public int Stacks;
    }

    /// <summary>A ring card: applying From may also apply To without triggering another ring link.</summary>
    public struct StatusRingLink
    {
        public StatusType From;
        public StatusType To;
        public float Chance;
        public int Stacks;
    }

    /// <summary>
    /// Everything the run's card choices have built: equipped skills, tag and skill bonuses, procs, status boosts, ring links, reactions, hearts, legendaries.
    /// Plain C#, no Unity calls besides reading <see cref="SkillDefinition"/> fields, so it is unit-testable.
    /// The card pool reads it to decide which cards are eligible (docs/CONCEPT.md, "Карта карточек").
    /// </summary>
    public sealed class BuildState
    {
        private static readonly int ElementCount = Enum.GetValues(typeof(Element)).Length;
        private static readonly int FormCount = Enum.GetValues(typeof(SkillForm)).Length;
        private static readonly int StatCount = Enum.GetValues(typeof(SkillStat)).Length;
        private static readonly int StatusCount = Enum.GetValues(typeof(StatusType)).Length;

        private readonly List<SkillDefinition> skills = new List<SkillDefinition>();
        private readonly List<int> skillLevels = new List<int>(); // parallel to skills
        private readonly float[] elementBonus = new float[ElementCount * StatCount];
        private readonly float[] formBonus = new float[FormCount * StatCount];
        private readonly float[] chainChance = new float[FormCount * FormCount];
        private readonly Dictionary<SkillDefinition, float[]> skillBonus = new Dictionary<SkillDefinition, float[]>();
        private readonly List<StatusProc> procs = new List<StatusProc>();
        private readonly List<StatusRingLink> rings = new List<StatusRingLink>();
        private readonly int[] statusSourceCards = new int[StatusCount];
        private readonly float[] boosts = new float[Enum.GetValues(typeof(StatusBoostType)).Length];
        private readonly bool[] reactions = new bool[Enum.GetValues(typeof(ReactionType)).Length];
        private readonly bool[] hearts = new bool[ElementCount];
        private readonly bool[] legendaries = new bool[Enum.GetValues(typeof(LegendaryType)).Length];
        private readonly HashSet<PowerDefinition> taken = new HashSet<PowerDefinition>();

        [Inject]
        public BuildState(PlayerConfig config) : this(config.SkillSlots) { }

        public BuildState(int skillSlots)
        {
            SkillSlots = skillSlots;
        }

        public int SkillSlots { get; }

        /// <summary>Executioner rune: extra damage per different status on the target (0.05 = +5% each).</summary>
        public float DamagePerTargetStatus { get; set; }
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public IReadOnlyList<StatusProc> Procs => procs;
        public IReadOnlyList<StatusRingLink> Rings => rings;
        public bool HasFreeSlot => skills.Count < SkillSlots;

        // ---------- Skills ----------

        public void AddSkill(SkillDefinition skill)
        {
            skills.Add(skill);
            skillLevels.Add(1);
        }

        /// <summary>Level of an equipped skill (1 when added), 0 if not equipped (docs/CONCEPT.md, «Уровень умения»).</summary>
        public int SkillLevel(SkillDefinition skill)
        {
            int i = skills.IndexOf(skill);
            return i < 0 ? 0 : skillLevels[i];
        }

        /// <summary>
        /// Raises the level of every equipped skill the card changes: its own upgrade, its element tag or its form tag.
        /// Skills equipped later keep the tag bonus but get no level from earlier cards.
        /// </summary>
        public void RaiseLevels(PowerDefinition power)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition s = skills[i];
                bool hit = (power.Kind == PowerKind.SkillUpgrade && power.Skill == s)
                    || (power.Kind == PowerKind.ElementUpgrade && power.Element == s.Element)
                    || (power.Kind == PowerKind.FormUpgrade && power.Form == s.Form);
                if (hit) skillLevels[i]++;
            }
        }

        public bool HasSkill(SkillDefinition skill) => skill != null && skills.Contains(skill);

        public int CountElement(Element element)
        {
            int n = 0;
            for (int i = 0; i < skills.Count; i++) if (skills[i].Element == element) n++;
            return n;
        }

        public int CountForm(SkillForm form)
        {
            int n = 0;
            for (int i = 0; i < skills.Count; i++) if (skills[i].Form == form) n++;
            return n;
        }

        /// <summary>Skills that apply the status plus proc and ring cards for it (docs/CONCEPT.md: a source is a skill or a card).</summary>
        public int StatusSources(StatusType status)
        {
            if (status == StatusType.None) return 0;
            int n = statusSourceCards[(int)status];
            for (int i = 0; i < skills.Count; i++) if (skills[i].Status == status) n++;
            return n;
        }

        // ---------- Bonuses ----------

        public void AddElementBonus(Element element, SkillStat stat, float value) => elementBonus[(int)element * StatCount + (int)stat] += value;

        public float ElementBonus(Element element, SkillStat stat) => elementBonus[(int)element * StatCount + (int)stat];

        public float FormBonus(SkillForm form, SkillStat stat) => formBonus[(int)form * StatCount + (int)stat];

        public void AddFormBonus(SkillForm form, SkillStat stat, float value) => formBonus[(int)form * StatCount + (int)stat] += value;

        public void AddSkillBonus(SkillDefinition skill, SkillStat stat, float value)
        {
            if (!skillBonus.TryGetValue(skill, out float[] values))
            {
                values = new float[StatCount];
                skillBonus.Add(skill, values);
            }
            values[(int)stat] += value;
        }

        /// <summary>Element + form + own bonus for one skill stat. General player stats are added by the skill itself.</summary>
        public float Bonus(SkillDefinition skill, SkillStat stat)
        {
            int s = (int)stat;
            float sum = elementBonus[(int)skill.Element * StatCount + s] + formBonus[(int)skill.Form * StatCount + s];
            if (skillBonus.TryGetValue(skill, out float[] own)) sum += own[s];
            return sum;
        }

        // ---------- Chains ----------

        public void AddChain(SkillForm from, SkillForm to, float chance)
        {
            if (from == to) return;
            chainChance[(int)from * FormCount + (int)to] += chance;
        }

        public float ChainChance(SkillForm from, SkillForm to) => chainChance[(int)from * FormCount + (int)to];

        // ---------- Statuses ----------

        public void AddProc(StatusProc proc)
        {
            procs.Add(proc);
            if (proc.Status != StatusType.None) statusSourceCards[(int)proc.Status]++;
        }

        public void AddRing(StatusRingLink link)
        {
            rings.Add(link);
            if (link.To != StatusType.None) statusSourceCards[(int)link.To]++;
        }

        public void AddBoost(StatusBoostType boost, float value) => boosts[(int)boost] += value;

        public float Boost(StatusBoostType boost) => boosts[(int)boost];

        public void UnlockReaction(ReactionType reaction) => reactions[(int)reaction] = true;

        public bool HasReaction(ReactionType reaction) => reactions[(int)reaction];

        public int ReactionCount()
        {
            int count = 0;
            for (int i = 0; i < reactions.Length; i++) if (reactions[i]) count++;
            return count;
        }

        public void UnlockHeart(Element e) => hearts[(int)e] = true;

        public bool HasHeart(Element e) => hearts[(int)e];

        public void UnlockLegendary(LegendaryType legendary) => legendaries[(int)legendary] = true;

        public bool HasLegendary(LegendaryType legendary) => legendaries[(int)legendary];

        // ---------- Cards ----------

        public void MarkTaken(PowerDefinition power) => taken.Add(power);

        public bool WasTaken(PowerDefinition power) => taken.Contains(power);

        /// <summary>True if the card may appear in an offer now: not a taken unique card and every condition holds.</summary>
        public bool IsEligible(PowerDefinition power)
        {
            if (power.Unique && taken.Contains(power)) return false;
            PowerCondition[] conditions = power.Conditions;
            if (conditions == null) return true;
            for (int i = 0; i < conditions.Length; i++)
            {
                if (!Satisfies(conditions[i])) return false;
            }
            return true;
        }

        public bool Satisfies(PowerCondition condition)
        {
            switch (condition.Type)
            {
                case ConditionType.HasFreeSlot: return HasFreeSlot;
                case ConditionType.SkillNotTaken: return !HasSkill(condition.Skill);
                case ConditionType.HasSkill: return HasSkill(condition.Skill);
                case ConditionType.MinSkillsWithElement: return CountElement(condition.Element) >= condition.Count;
                case ConditionType.MinSkillsWithForm: return CountForm(condition.Form) >= condition.Count;
                case ConditionType.MinStatusSources: return StatusSources(condition.Status) >= condition.Count;
                case ConditionType.HasHeart: return HasHeart(condition.Element);
                case ConditionType.MinReactions: return ReactionCount() >= condition.Count;
                default: return true;
            }
        }
    }
}
