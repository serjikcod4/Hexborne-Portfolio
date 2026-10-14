# Cards that follow the current build

Start with [CardRoller.cs](../Samples/CardRoller.cs) and [BuildState.cs](../Samples/BuildState.cs). [PowerDefinition](../Samples/PowerDefinition.cs), [PowerPoolConfig](../Samples/PowerPoolConfig.cs), [SkillDefinition](../Samples/SkillDefinition.cs), [Enums](../Samples/Enums.cs) and [RandomSource](../Samples/RandomSource.cs) provide the data contracts.

`BuildState` owns equipped skills, skill levels, additive bonuses, status sources, unlocked reactions, Hearts and legendaries. A card is eligible when every condition passes and a unique card has not already been taken. Status sources include equipped skills and registered proc/ring cards.

`CardRoller` rolls a rarity by configured weights, chooses uniformly among eligible cards of that rarity, then searches downward to the offer's minimum and upward if no card was found. Therefore a card's final probability depends on the current pool; a rarity weight is not a fixed per-card drop chance. The default weights total 92 and are relative weights, not percentages.

Ordinary power offers exclude `NewSkill`. Skill choices prioritize new skills uniformly when a slot is free; with full slots they prioritize upgrades. Both paths fill shortages with powers. Each offer excludes its own previous picks and every banished card. An exhausted pool returns a smaller offer.

`RollKeeping` preserves a caller-validated card and its slot where possible. The surrounding offer controller owns lock charges and carrying locks into later offers; it is outside this extract. `BanAndReplace` preserves order or shrinks the offer when no replacement exists. `Boost` can insert an eligible Resonance card while protecting the retained slot and avoiding random draws when no eligible candidate exists.

Randomness is injected through `IRandomSource`. [TestRandom.cs](../Tests/TestRandom.cs) queues explicit rolls and falls back to a seeded generator. [CardRollerTests](../Tests/CardRollerTests.cs) cover distinct offers, rarity fallback, bans, minimum rarity, skill-choice behavior, conditions, uniqueness and the protected slot during boosting. [BuildStateTests](../Tests/BuildStateTests.cs) cover additive bonuses, source counts and eligibility.

These offers occur while selecting upgrades, not in the per-frame simulation. Their lists, sets and ScriptableObjects are an intentional convenience; the hot-layer allocation policy does not imply this subsystem allocates nothing. Configurations are expected to have valid positive rarity weights and appropriately sized output buffers.

The files retain production injection constructors. `PlayerConfig`, `SoundId` and Zenject belong to integration outside this extract; see [validation](validation.md).
