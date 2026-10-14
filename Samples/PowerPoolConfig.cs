using UnityEngine;

namespace Hexborne.Gameplay
{
    [CreateAssetMenu(menuName = "Hexborne/Power Pool Config", fileName = "PowerPoolConfig")]
    public sealed class PowerPoolConfig : ScriptableObject
    {
        [Tooltip("Weights by rarity: Common, Uncommon, Rare, Epic, Legendary.")]
        public int[] RarityWeights = { 50, 25, 12, 4, 1 };
        public int CardsPerOffer = 3;
        [Header("Per run, before Altar upgrades")]
        public int RerollsPerRun = 2;
        public int BansPerRun = 1;
        [Tooltip("Locks: a locked card stays on reroll and returns in the next offer of the same kind.")]
        public int LocksPerRun = 2;
        [Tooltip("Power offers and chests: if a Resonance card is eligible and the offer has none, this chance puts one in place of a random card. The Epic roll alone (4%) made them too rare.")]
        [Range(0f, 1f)] public float ResonanceBoostChance = 0.35f;
        public PowerDefinition[] Powers = new PowerDefinition[0];
    }
}
