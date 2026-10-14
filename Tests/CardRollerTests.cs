using System.Collections.Generic;
using Hexborne.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hexborne.Tests
{
    public sealed class CardRollerTests
    {
        private static readonly int[] DefaultWeights = { 50, 25, 12, 4, 1 };
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Roll_ReturnsDistinctCards()
        {
            var pool = new[] { Card(Rarity.Common), Card(Rarity.Common), Card(Rarity.Common), Card(Rarity.Rare) };
            var roller = new CardRoller(pool, DefaultWeights, new BuildState(5), new SystemRandomSource(7));
            var output = new PowerDefinition[3];

            for (int attempt = 0; attempt < 200; attempt++)
            {
                int n = roller.Roll(output, 3);
                Assert.AreEqual(3, n);
                Assert.AreNotSame(output[0], output[1]);
                Assert.AreNotSame(output[0], output[2]);
                Assert.AreNotSame(output[1], output[2]);
            }
        }

        [Test]
        public void Roll_StepsRarityDown_WhenRolledRarityIsMissing()
        {
            PowerDefinition common = Card(Rarity.Common);
            var pool = new[] { common };
            // Range(0, 92) → 91 lands in Legendary; then Range(0, 1) picks the only candidate.
            var random = new QueueRandom().WithInts(91, 0);
            var roller = new CardRoller(pool, DefaultWeights, new BuildState(5), random);
            var output = new PowerDefinition[1];

            Assert.AreEqual(1, roller.Roll(output, 1));
            Assert.AreSame(common, output[0]);
        }

        [Test]
        public void Roll_StepsUp_WhenNothingBelowIsLeft()
        {
            PowerDefinition epic = Card(Rarity.Epic);
            var roller = new CardRoller(new[] { epic }, DefaultWeights, new BuildState(5), new QueueRandom().WithInts(0, 0));
            var output = new PowerDefinition[1];

            Assert.AreEqual(1, roller.Roll(output, 1));
            Assert.AreSame(epic, output[0]);
        }

        [Test]
        public void Roll_ReturnsFewerCards_WhenPoolIsSmall()
        {
            var roller = new CardRoller(new[] { Card(Rarity.Common), Card(Rarity.Rare) }, DefaultWeights, new BuildState(5), new SystemRandomSource(3));
            Assert.AreEqual(2, roller.Roll(new PowerDefinition[3], 3));
        }

        [Test]
        public void BanishedCard_NeverAppears()
        {
            PowerDefinition banned = Card(Rarity.Common);
            PowerDefinition other = Card(Rarity.Common);
            var roller = new CardRoller(new[] { banned, other }, DefaultWeights, new BuildState(5), new SystemRandomSource(11));
            roller.Banish(banned);
            var output = new PowerDefinition[3];

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int n = roller.Roll(output, 3);
                Assert.AreEqual(1, n);
                Assert.AreSame(other, output[0]);
            }
        }

        [Test]
        public void RollRarity_FollowsWeights()
        {
            var roller = new CardRoller(new PowerDefinition[0], DefaultWeights, new BuildState(5), new SystemRandomSource(42));
            var counts = new int[5];
            const int samples = 20000;
            for (int i = 0; i < samples; i++) counts[(int)roller.RollRarity()]++;

            Assert.AreEqual(50f / 92f, counts[0] / (float)samples, 0.02f);
            Assert.AreEqual(25f / 92f, counts[1] / (float)samples, 0.02f);
            Assert.AreEqual(12f / 92f, counts[2] / (float)samples, 0.015f);
            Assert.AreEqual(4f / 92f, counts[3] / (float)samples, 0.01f);
            Assert.AreEqual(1f / 92f, counts[4] / (float)samples, 0.005f);
        }

        [Test]
        public void Roll_NeverOffersNewSkill()
        {
            PowerDefinition newSkill = NewSkillCard(Skill(Element.Shadow, SkillForm.Projectile));
            var pool = new[] { Card(Rarity.Common), Card(Rarity.Common), Card(Rarity.Uncommon), newSkill };
            var roller = new CardRoller(pool, DefaultWeights, new BuildState(6), new SystemRandomSource(5));
            var output = new PowerDefinition[3];

            for (int attempt = 0; attempt < 200; attempt++)
            {
                Assert.AreEqual(3, roller.Roll(output, 3));
                CollectionAssert.DoesNotContain(output, newSkill);
            }
        }

        [Test]
        public void RollSkillChoice_OffersOnlyNewSkills_WhileSlotIsFree()
        {
            var skills = new[]
            {
                NewSkillCard(Skill(Element.Fire, SkillForm.Area)), NewSkillCard(Skill(Element.Ice, SkillForm.Aura)),
                NewSkillCard(Skill(Element.Shadow, SkillForm.Projectile)), NewSkillCard(Skill(Element.Ice, SkillForm.Area))
            };
            var pool = new List<PowerDefinition>(skills) { Card(Rarity.Common), Card(Rarity.Common), Card(Rarity.Common) };
            var roller = new CardRoller(pool, DefaultWeights, new BuildState(6), new SystemRandomSource(6));
            var output = new PowerDefinition[3];

            for (int attempt = 0; attempt < 100; attempt++)
            {
                Assert.AreEqual(3, roller.RollSkillChoice(output, 3));
                for (int i = 0; i < 3; i++) Assert.AreEqual(PowerKind.NewSkill, output[i].Kind);
                Assert.AreNotSame(output[0], output[1]);
                Assert.AreNotSame(output[1], output[2]);
                Assert.AreNotSame(output[0], output[2]);
            }
        }

        [Test]
        public void RollSkillChoice_IgnoresRarity()
        {
            PowerDefinition common = NewSkillCard(Skill(Element.Fire, SkillForm.Area));
            common.Rarity = Rarity.Common;
            PowerDefinition legendary = NewSkillCard(Skill(Element.Ice, SkillForm.Aura));
            legendary.Rarity = Rarity.Legendary;
            var roller = new CardRoller(new[] { common, legendary }, DefaultWeights, new BuildState(6), new SystemRandomSource(12));
            var output = new PowerDefinition[1];
            int legendaryCount = 0;

            for (int attempt = 0; attempt < 400; attempt++)
            {
                Assert.AreEqual(1, roller.RollSkillChoice(output, 1));
                if (output[0] == legendary) legendaryCount++;
            }
            Assert.AreEqual(0.5f, legendaryCount / 400f, 0.1f);
        }

        [Test]
        public void RollSkillChoice_OffersSkillUpgrades_WhenSlotsAreFull()
        {
            SkillDefinition owned = Skill(Element.Fire, SkillForm.Projectile);
            var build = new BuildState(1);
            build.AddSkill(owned);
            PowerDefinition upgrade = Card(Rarity.Uncommon);
            upgrade.Kind = PowerKind.SkillUpgrade;
            upgrade.Skill = owned;
            upgrade.Conditions = new[] { new PowerCondition { Type = ConditionType.HasSkill, Skill = owned } };
            PowerDefinition newSkill = NewSkillCard(Skill(Element.Ice, SkillForm.Area));
            PowerDefinition stat = Card(Rarity.Common);
            var roller = new CardRoller(new[] { upgrade, newSkill, stat }, DefaultWeights, build, new SystemRandomSource(3));
            var output = new PowerDefinition[1];

            for (int attempt = 0; attempt < 50; attempt++)
            {
                Assert.AreEqual(1, roller.RollSkillChoice(output, 1));
                Assert.AreSame(upgrade, output[0]);
            }
        }

        [Test]
        public void RollSkillChoice_FillsWithPowers_WhenFewSkillsLeft()
        {
            PowerDefinition onlySkill = NewSkillCard(Skill(Element.Shadow, SkillForm.Aura));
            var roller = new CardRoller(new[] { onlySkill, Card(Rarity.Common), Card(Rarity.Common), Card(Rarity.Common) },
                DefaultWeights, new BuildState(6), new SystemRandomSource(21));
            var output = new PowerDefinition[3];

            Assert.AreEqual(3, roller.RollSkillChoice(output, 3));
            Assert.AreSame(onlySkill, output[0]);
            Assert.AreEqual(PowerKind.Stat, output[1].Kind);
            Assert.AreEqual(PowerKind.Stat, output[2].Kind);
        }

        [Test]
        public void BanAndReplace_BanishesAndReplaces()
        {
            PowerDefinition a = Card(Rarity.Common), b = Card(Rarity.Common), c = Card(Rarity.Common), d = Card(Rarity.Common);
            var roller = new CardRoller(new[] { a, b, c, d }, DefaultWeights, new BuildState(6), new SystemRandomSource(31));
            var output = new[] { a, b, c };
            int count = 3;

            Assert.IsTrue(roller.BanAndReplace(output, ref count, 1, false, Rarity.Common));
            Assert.AreEqual(3, count);
            Assert.AreSame(a, output[0]);
            Assert.AreSame(d, output[1]);
            Assert.AreSame(c, output[2]);
            Assert.IsTrue(roller.IsBanished(b));
            for (int attempt = 0; attempt < 50; attempt++)
            {
                roller.Roll(output, 3);
                CollectionAssert.DoesNotContain(output, b);
            }
        }

        [Test]
        public void BanAndReplace_ShrinksOffer_WhenNothingLeft()
        {
            PowerDefinition a = Card(Rarity.Common), b = Card(Rarity.Common), c = Card(Rarity.Common);
            var roller = new CardRoller(new[] { a, b, c }, DefaultWeights, new BuildState(6), new SystemRandomSource(32));
            var output = new[] { a, b, c };
            int count = 3;

            Assert.IsFalse(roller.BanAndReplace(output, ref count, 0, false, Rarity.Common));
            Assert.AreEqual(2, count);
            Assert.AreSame(b, output[0]);
            Assert.AreSame(c, output[1]);
            Assert.IsNull(output[2]);
        }

        [Test]
        public void BanAndReplace_InSkillChoice_ReplacesWithAnotherNewSkill()
        {
            PowerDefinition s1 = NewSkillCard(Skill(Element.Fire, SkillForm.Area));
            PowerDefinition s2 = NewSkillCard(Skill(Element.Ice, SkillForm.Area));
            PowerDefinition s3 = NewSkillCard(Skill(Element.Shadow, SkillForm.Area));
            PowerDefinition s4 = NewSkillCard(Skill(Element.Shadow, SkillForm.Aura));
            var roller = new CardRoller(new[] { s1, s2, s3, s4, Card(Rarity.Common) }, DefaultWeights, new BuildState(6), new SystemRandomSource(33));
            var output = new[] { s1, s2, s3 };
            int count = 3;

            Assert.IsTrue(roller.BanAndReplace(output, ref count, 2, true, Rarity.Common));
            Assert.AreSame(s4, output[2]);
        }

        [Test]
        public void Roll_RespectsMinimumRarity()
        {
            PowerDefinition rare = Card(Rarity.Rare);
            var roller = new CardRoller(new[] { Card(Rarity.Common), Card(Rarity.Uncommon), rare }, DefaultWeights, new BuildState(5), new SystemRandomSource(2));
            var output = new PowerDefinition[3];

            for (int attempt = 0; attempt < 50; attempt++)
            {
                Assert.AreEqual(1, roller.Roll(output, 3, Rarity.Rare));
                Assert.AreSame(rare, output[0]);
            }
        }

        [Test]
        public void CardWithUnmetCondition_NeverAppears_UntilConditionHolds()
        {
            SkillDefinition skill = Skill(Element.Fire, SkillForm.Area);
            PowerDefinition upgrade = Card(Rarity.Common);
            upgrade.Kind = PowerKind.SkillUpgrade;
            upgrade.Skill = skill;
            upgrade.Conditions = new[] { new PowerCondition { Type = ConditionType.HasSkill, Skill = skill } };
            var build = new BuildState(5);
            var roller = new CardRoller(new[] { upgrade, Card(Rarity.Common) }, DefaultWeights, build, new SystemRandomSource(4));
            var output = new PowerDefinition[2];

            Assert.AreEqual(1, roller.Roll(output, 2));
            build.AddSkill(skill);
            Assert.AreEqual(2, roller.Roll(output, 2));
        }

        [Test]
        public void UniqueCard_NotOfferedAfterItWasTaken()
        {
            PowerDefinition unique = Card(Rarity.Common);
            unique.Unique = true;
            var build = new BuildState(5);
            var roller = new CardRoller(new[] { unique, Card(Rarity.Common) }, DefaultWeights, build, new SystemRandomSource(8));
            var output = new PowerDefinition[2];

            Assert.AreEqual(2, roller.Roll(output, 2));
            build.MarkTaken(unique);
            Assert.AreEqual(1, roller.Roll(output, 2));
            Assert.AreNotSame(unique, output[0]);
        }

        // ---------- Resonance boost ----------

        [Test]
        public void Boost_PutsEligibleCardInPlace_SkippingTheKeptSlot()
        {
            PowerDefinition a = Card(Rarity.Common), b = Card(Rarity.Common), c = Card(Rarity.Common);
            PowerDefinition resonance = ResonanceCard();
            // Value01 0.1 < 0.35 → boost; Range(0, 2) = 0 is shifted past kept slot 0 → slot 1; Range(0, 1) picks the card.
            var random = new QueueRandom().WithValues(0.1f).WithInts(0, 0);
            var roller = new CardRoller(new[] { a, b, c, resonance }, DefaultWeights, new BuildState(5), random);
            var output = new[] { a, b, c };

            Assert.IsTrue(roller.Boost(output, 3, PowerKind.Resonance, 0.35f, keepIndex: 0));
            Assert.AreSame(a, output[0], "the locked card stays");
            Assert.AreSame(resonance, output[1]);
            Assert.AreSame(c, output[2]);
        }

        [Test]
        public void Boost_MissedChance_KeepsTheOffer()
        {
            PowerDefinition a = Card(Rarity.Common), b = Card(Rarity.Common);
            var roller = new CardRoller(new[] { a, b, ResonanceCard() }, DefaultWeights, new BuildState(5), new QueueRandom().WithValues(0.5f));
            var output = new[] { a, b };

            Assert.IsFalse(roller.Boost(output, 2, PowerKind.Resonance, 0.35f));
            CollectionAssert.AreEqual(new[] { a, b }, output);
        }

        [Test]
        public void Boost_NothingEligible_DrawsNothing()
        {
            PowerDefinition a = Card(Rarity.Common);
            PowerDefinition resonance = ResonanceCard();
            resonance.Conditions = new[] { new PowerCondition { Type = ConditionType.MinStatusSources, Status = StatusType.Burn, Count = 1 } };
            var random = new QueueRandom().WithValues(0.123f);
            var roller = new CardRoller(new[] { a, resonance }, DefaultWeights, new BuildState(5), random);
            var output = new[] { a };

            Assert.IsFalse(roller.Boost(output, 1, PowerKind.Resonance, 1f));
            Assert.AreEqual(0.123f, random.Value01(), "offers without an eligible card roll exactly as before");
        }

        [Test]
        public void Boost_OfferAlreadyHasOne_OrBannedCard_DoesNothing()
        {
            PowerDefinition a = Card(Rarity.Common);
            PowerDefinition first = ResonanceCard(), second = ResonanceCard();
            var roller = new CardRoller(new[] { a, first, second }, DefaultWeights, new BuildState(5), new QueueRandom().WithValues(0f, 0f));
            var output = new[] { a, first };
            Assert.IsFalse(roller.Boost(output, 2, PowerKind.Resonance, 1f), "one Resonance card per offer is enough");

            roller.Banish(first);
            roller.Banish(second);
            output = new[] { a };
            Assert.IsFalse(roller.Boost(output, 1, PowerKind.Resonance, 1f));
            Assert.AreSame(a, output[0]);
        }

        private PowerDefinition ResonanceCard()
        {
            PowerDefinition card = Card(Rarity.Epic);
            card.Kind = PowerKind.Resonance;
            card.Unique = true;
            return card;
        }

        private PowerDefinition NewSkillCard(SkillDefinition skill)
        {
            PowerDefinition card = Card(Rarity.Uncommon);
            card.Kind = PowerKind.NewSkill;
            card.Unique = true;
            card.Skill = skill;
            card.Conditions = new[]
            {
                new PowerCondition { Type = ConditionType.HasFreeSlot },
                new PowerCondition { Type = ConditionType.SkillNotTaken, Skill = skill }
            };
            return card;
        }

        private SkillDefinition Skill(Element element, SkillForm form)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.Element = element;
            skill.Form = form;
            created.Add(skill);
            return skill;
        }

        private PowerDefinition Card(Rarity rarity)
        {
            var card = ScriptableObject.CreateInstance<PowerDefinition>();
            card.Rarity = rarity;
            card.Id = rarity + "_" + created.Count;
            created.Add(card);
            return card;
        }
    }
}
