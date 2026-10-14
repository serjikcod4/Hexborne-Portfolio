using System.Collections.Generic;
using Zenject;

namespace Hexborne.Gameplay
{
    /// <summary>
    /// Rolls offers (docs/CONCEPT.md, «Как бросается карточка»).
    /// Power offer (normal levels, chests): rarity by weight, then a uniform pick among eligible cards of that rarity;
    /// if none fit, steps the rarity down (not below the minimum), then up. Never contains NewSkill cards.
    /// Skill choice (every SkillChoiceEvery-th level): NewSkill cards picked uniformly, ignoring rarity; with no free slot,
    /// SkillUpgrade cards by the usual rarity roll; the rest is filled with a power roll.
    /// No duplicates in one offer. Eligibility comes from <see cref="BuildState.IsEligible"/>; banished cards never appear.
    /// </summary>
    public sealed class CardRoller
    {
        private enum Pool { Powers, NewSkills, SkillUpgrades }

        private readonly IReadOnlyList<PowerDefinition> powers;
        private readonly int[] weights;
        private readonly BuildState build;
        private readonly IRandomSource random;
        private readonly HashSet<PowerDefinition> banished = new HashSet<PowerDefinition>();
        private readonly List<PowerDefinition> candidates = new List<PowerDefinition>();
        private readonly int totalWeight;

        [Inject]
        public CardRoller(PowerPoolConfig config, BuildState build, IRandomSource random)
            : this(config.Powers, config.RarityWeights, build, random) { }

        public CardRoller(IReadOnlyList<PowerDefinition> powers, int[] weights, BuildState build, IRandomSource random)
        {
            this.powers = powers;
            this.weights = weights;
            this.build = build;
            this.random = random;
            for (int i = 0; i < weights.Length; i++) totalWeight += weights[i];
        }

        public void Banish(PowerDefinition power) => banished.Add(power);

        public bool IsBanished(PowerDefinition power) => banished.Contains(power);

        public Rarity RollRarity()
        {
            int roll = random.Range(0, totalWeight);
            for (int i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i]) return (Rarity)i;
                roll -= weights[i];
            }
            return Rarity.Common;
        }

        /// <summary>
        /// Power offer: fills <paramref name="output"/> with up to <paramref name="count"/> distinct cards and returns how many were filled.
        /// <paramref name="minRarity"/>: no card below this rarity (chests use Rare). NewSkill cards never appear.
        /// </summary>
        public int Roll(PowerDefinition[] output, int count, Rarity minRarity = Rarity.Common) => FillPowers(output, 0, count, minRarity);

        private int FillPowers(PowerDefinition[] output, int filled, int count, Rarity minRarity)
        {
            while (filled < count)
            {
                PowerDefinition picked = PickByRarity(Pool.Powers, minRarity, output, filled);
                if (picked == null) break;
                output[filled++] = picked;
            }
            return filled;
        }

        /// <summary>
        /// Skill choice: with a free slot, up to <paramref name="count"/> NewSkill cards picked uniformly, ignoring rarity;
        /// with no free slot, SkillUpgrade cards by rarity. If fewer than <paramref name="count"/> were found, the rest is a power roll.
        /// </summary>
        public int RollSkillChoice(PowerDefinition[] output, int count) => FillSkillChoice(output, 0, count);

        /// <summary>
        /// Lock: an offer of the given mode with <paramref name="kept"/> at <paramref name="keptIndex"/> (or the last slot if fewer
        /// cards were found) and the other slots rolled as usual, never duplicating it. The caller checks <see cref="CanOffer"/> first.
        /// </summary>
        public int RollKeeping(PowerDefinition[] output, int count, PowerDefinition kept, int keptIndex, bool skillChoice, Rarity minRarity)
        {
            output[0] = kept;
            int filled = skillChoice ? FillSkillChoice(output, 1, count) : FillPowers(output, 1, count, minRarity);
            int at = keptIndex < filled ? keptIndex : filled - 1;
            if (at > 0)
            {
                output[0] = output[at];
                output[at] = kept;
            }
            return filled;
        }

        /// <summary>
        /// Affinity rune: if the offer has no NewSkill card of <paramref name="element"/> but one is eligible, it replaces the first card.
        /// Returns true when the offer ends up with such a card.
        /// </summary>
        public bool EnsureElement(PowerDefinition[] output, int count, Element element)
        {
            if (count <= 0) return false;
            for (int i = 0; i < count; i++)
            {
                if (output[i] != null && output[i].Kind == PowerKind.NewSkill && output[i].Skill != null && output[i].Skill.Element == element) return true;
            }
            candidates.Clear();
            for (int i = 0; i < powers.Count; i++)
            {
                PowerDefinition p = powers[i];
                if (p != null && p.Kind == PowerKind.NewSkill && p.Skill != null && p.Skill.Element == element && IsCandidate(p, output, count)) candidates.Add(p);
            }
            if (candidates.Count == 0) return false;
            output[0] = candidates[random.Range(0, candidates.Count)];
            return true;
        }

        /// <summary>
        /// Boost (Resonance): if a card of <paramref name="kind"/> is eligible and the offer has none, with <paramref name="chance"/>
        /// it replaces a random card other than the one at <paramref name="keepIndex"/> (the locked card). No random draw when nothing
        /// of the kind is eligible, so offers without it roll exactly as before. Returns true when a card was put in.
        /// </summary>
        public bool Boost(PowerDefinition[] output, int count, PowerKind kind, float chance, int keepIndex = -1)
        {
            if (count <= 0 || chance <= 0f) return false;
            bool keep = keepIndex >= 0 && keepIndex < count;
            int slots = keep ? count - 1 : count;
            if (slots <= 0) return false;
            for (int i = 0; i < count; i++)
            {
                if (output[i] != null && output[i].Kind == kind) return false;
            }
            candidates.Clear();
            for (int i = 0; i < powers.Count; i++)
            {
                PowerDefinition p = powers[i];
                if (p != null && p.Kind == kind && IsCandidate(p, output, count)) candidates.Add(p);
            }
            if (candidates.Count == 0) return false;
            if (random.Value01() >= chance) return false;
            int slot = random.Range(0, slots);
            if (keep && slot >= keepIndex) slot++;
            output[slot] = candidates[random.Range(0, candidates.Count)];
            return true;
        }

        /// <summary>True if the card may appear in an offer of this mode now: not banished, eligible, no NewSkill in power offers.</summary>
        public bool CanOffer(PowerDefinition card, bool skillChoice) =>
            card != null && !banished.Contains(card) && build.IsEligible(card) && (skillChoice || card.Kind != PowerKind.NewSkill);

        private int FillSkillChoice(PowerDefinition[] output, int filled, int count)
        {
            while (filled < count)
            {
                PowerDefinition picked = PickSkillChoice(output, filled);
                if (picked == null) break;
                output[filled++] = picked;
            }
            while (filled < count)
            {
                PowerDefinition picked = PickByRarity(Pool.Powers, Rarity.Common, output, filled);
                if (picked == null) break;
                output[filled++] = picked;
            }
            return filled;
        }

        /// <summary>
        /// Ban: banishes <c>output[index]</c> for the rest of the run and replaces it with a new card of the same offer mode.
        /// If nothing can replace it, the slot is removed, later cards shift left and <paramref name="count"/> shrinks; returns false then.
        /// </summary>
        public bool BanAndReplace(PowerDefinition[] output, ref int count, int index, bool skillChoice, Rarity minRarity)
        {
            if (index < 0 || index >= count) return false;
            banished.Add(output[index]);

            // Exclude every other card of the offer from the replacement pick.
            PowerDefinition last = output[count - 1];
            output[index] = last;
            int others = count - 1;

            PowerDefinition replacement = null;
            if (skillChoice) replacement = PickSkillChoice(output, others);
            if (replacement == null) replacement = PickByRarity(Pool.Powers, minRarity, output, others);

            // Restore the original order: the replacement takes the banned card's place.
            output[count - 1] = last;
            if (replacement != null)
            {
                output[index] = replacement;
                return true;
            }

            for (int i = index; i < count - 1; i++) output[i] = output[i + 1];
            count--;
            output[count] = null;
            return false;
        }

        private PowerDefinition PickSkillChoice(PowerDefinition[] chosen, int chosenCount)
        {
            if (build.HasFreeSlot)
            {
                candidates.Clear();
                for (int i = 0; i < powers.Count; i++)
                {
                    PowerDefinition p = powers[i];
                    if (InPool(p, Pool.NewSkills) && IsCandidate(p, chosen, chosenCount)) candidates.Add(p);
                }
                return candidates.Count == 0 ? null : candidates[random.Range(0, candidates.Count)];
            }
            return PickByRarity(Pool.SkillUpgrades, Rarity.Common, chosen, chosenCount);
        }

        private PowerDefinition PickByRarity(Pool pool, Rarity minRarity, PowerDefinition[] chosen, int chosenCount)
        {
            int min = (int)minRarity;
            int max = (int)Rarity.Legendary;
            int rolled = (int)RollRarity();
            if (rolled < min) rolled = min;
            PowerDefinition picked = null;
            for (int r = rolled; r >= min && picked == null; r--) picked = PickOfRarity(pool, (Rarity)r, chosen, chosenCount);
            for (int r = rolled + 1; r <= max && picked == null; r++) picked = PickOfRarity(pool, (Rarity)r, chosen, chosenCount);
            return picked;
        }

        private static bool InPool(PowerDefinition p, Pool pool)
        {
            switch (pool)
            {
                case Pool.NewSkills: return p.Kind == PowerKind.NewSkill;
                case Pool.SkillUpgrades: return p.Kind == PowerKind.SkillUpgrade;
                default: return p.Kind != PowerKind.NewSkill;
            }
        }

        private bool IsCandidate(PowerDefinition p, PowerDefinition[] chosen, int chosenCount)
        {
            if (p == null || banished.Contains(p) || !build.IsEligible(p)) return false;
            for (int c = 0; c < chosenCount; c++)
            {
                if (chosen[c] == p) return false;
            }
            return true;
        }

        private PowerDefinition PickOfRarity(Pool pool, Rarity rarity, PowerDefinition[] chosen, int chosenCount)
        {
            candidates.Clear();
            for (int i = 0; i < powers.Count; i++)
            {
                PowerDefinition p = powers[i];
                if (p != null && p.Rarity == rarity && InPool(p, pool) && IsCandidate(p, chosen, chosenCount)) candidates.Add(p);
            }
            return candidates.Count == 0 ? null : candidates[random.Range(0, candidates.Count)];
        }
    }
}
