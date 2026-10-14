using System.Collections.Generic;
using Hexborne.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hexborne.Tests
{
    public sealed class BuildStateTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Bonus_SumsElementFormAndOwnBonus()
        {
            SkillDefinition fireball = Skill(Element.Fire, SkillForm.Projectile, StatusType.Burn);
            SkillDefinition scythe = Skill(Element.Shadow, SkillForm.Projectile, StatusType.Doom);
            var build = new BuildState(5);
            build.AddElementBonus(Element.Fire, SkillStat.Damage, 0.3f);
            build.AddFormBonus(SkillForm.Projectile, SkillStat.Damage, 0.4f);
            build.AddSkillBonus(fireball, SkillStat.Damage, 0.45f);

            Assert.AreEqual(1.15f, build.Bonus(fireball, SkillStat.Damage), 0.0001f);
            Assert.AreEqual(0.4f, build.Bonus(scythe, SkillStat.Damage), 0.0001f);
            Assert.AreEqual(0f, build.Bonus(fireball, SkillStat.Area), 0.0001f);
        }

        [Test]
        public void StatusSources_CountSkillsAndProcCards()
        {
            var build = new BuildState(5);
            build.AddSkill(Skill(Element.Fire, SkillForm.Projectile, StatusType.Burn));
            build.AddSkill(Skill(Element.Fire, SkillForm.Area, StatusType.Burn));
            build.AddSkill(Skill(Element.Shadow, SkillForm.Area, StatusType.None));
            build.AddProc(new StatusProc { Status = StatusType.Burn, Trigger = ProcTrigger.OnHit, Chance = 0.25f, Stacks = 1 });

            Assert.AreEqual(3, build.StatusSources(StatusType.Burn));
            Assert.AreEqual(0, build.StatusSources(StatusType.Doom));
        }

        [Test]
        public void Conditions_FollowTheBuild()
        {
            var build = new BuildState(2);
            SkillDefinition a = Skill(Element.Shadow, SkillForm.Projectile, StatusType.Doom);
            SkillDefinition b = Skill(Element.Shadow, SkillForm.Aura, StatusType.Doom);
            var twoShadow = new PowerCondition { Type = ConditionType.MinSkillsWithElement, Element = Element.Shadow, Count = 2 };
            var freeSlot = new PowerCondition { Type = ConditionType.HasFreeSlot };
            var notTakenB = new PowerCondition { Type = ConditionType.SkillNotTaken, Skill = b };
            var twoDoomSources = new PowerCondition { Type = ConditionType.MinStatusSources, Status = StatusType.Doom, Count = 2 };

            build.AddSkill(a);
            Assert.IsFalse(build.Satisfies(twoShadow));
            Assert.IsTrue(build.Satisfies(freeSlot));
            Assert.IsTrue(build.Satisfies(notTakenB));
            Assert.IsFalse(build.Satisfies(twoDoomSources));

            build.AddSkill(b);
            Assert.IsTrue(build.Satisfies(twoShadow));
            Assert.IsFalse(build.Satisfies(freeSlot));
            Assert.IsFalse(build.Satisfies(notTakenB));
            Assert.IsTrue(build.Satisfies(twoDoomSources));
        }

        private SkillDefinition Skill(Element element, SkillForm form, StatusType status)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.Element = element;
            skill.Form = form;
            skill.Status = status;
            created.Add(skill);
            return skill;
        }
    }
}
