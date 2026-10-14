using UnityEngine;

namespace Hexborne.Gameplay
{
    /// <summary>
    /// Data of one skill (docs/CONCEPT.md, "12 умений"). Each behaviour class reads only the fields it needs;
    /// the headers say which form uses what.
    /// </summary>
    [CreateAssetMenu(menuName = "Hexborne/Skill Definition", fileName = "Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        public string Id = "Fireball";
        public string DisplayName = "Fireball";
        [TextArea] public string Description;
        public Element Element;
        public SkillForm Form;

        [Header("Common")]
        [Tooltip("Base damage per hit (per tick for auras). 0 = the skill works only through its status.")]
        public float Damage = 20f;
        public float Cooldown = 1.2f;
        [Tooltip("Projectiles: target acquisition / flight distance. Areas: search radius for the densest spot.")]
        public float Range = 14f;
        public float MulticastInterval = 0.1f;
        [Tooltip("Status applied on every hit. A skill with a status counts as a source of it for card conditions.")]
        public StatusType Status;
        public int StatusStacks = 1;
        [Tooltip("A hit on a target that is frozen before this hit deals × (1 + this). Glacial Spikes: 1 = double damage. Bosses never freeze.")]
        public float FrozenDamageBonus;

        [Header("Projectile")]
        public float ProjectileSpeed = 14f;
        public float ProjectileLifetime = 1.6f;
        public float HitRadius = 0.35f;
        [Tooltip("Fireball explosion radius.")]
        public float ExplosionRadius = 1.5f;
        [Tooltip("Ice Lance: enemies one projectile hits before it breaks.")]
        public int MaxTargets = 1;
        [Tooltip("Area bonuses do not change this skill's size (Ice Lance: a wider lance only looked wider). Count cards add projectiles instead.")]
        public bool IgnoreArea;
        [Tooltip("Ice Lance volley: degrees between neighbouring lances when Count > 1.")]
        public float ConeStep = 12f;

        [Header("Area / Aura")]
        [Tooltip("Damage radius of an area blast, a zone or an aura. Frost Orbs: orbit radius.")]
        public float Radius = 2.5f;
        [Tooltip("Telegraph time before an area blast lands.")]
        public float Delay = 0.35f;
        [Tooltip("Lifetime of an aura or a lingering zone (Blizzard).")]
        public float Duration = 5f;
        [Tooltip("Aura / zone pulse interval. Frost Orbs: delay before an orb can hit the same enemy again.")]
        public float TickInterval = 0.5f;
        [Tooltip("Reap: enemies within this radius of the target spot are pulled in.")]
        public float PullRadius;
        [Tooltip("Reap: Doom banks of hit enemies detonate at this multiplier.")]
        public float DoomDetonateMultiplier;

        [Header("Orbit / Volley")]
        [Tooltip("Frost Orbs: number of orbs. Ice Lance / Doom Bolt: projectiles per cast before Volley cards. Glacial Spikes: spikes in the line.")]
        public int Count = 1;
        [Tooltip("Frost Orbs: orbit speed, degrees per second.")]
        public float AngularSpeed = 180f;

        [Header("Line / Trail")]
        [Tooltip("Glacial Spikes: metres between spikes (× area). Ember Trail: metres walked between fire patches.")]
        public float Spacing = 1.5f;
        [Tooltip("Glacial Spikes: seconds between consecutive spikes.")]
        public float StepInterval = 0.06f;
        [Tooltip("Ember Trail: lifetime of one fire patch (× duration).")]
        public float PatchLifetime = 2.5f;

        [Header("Sound")]
        [Tooltip("Played on every cast (multicast repeats, echoes and mirror casts too); the mixer limits how often.")]
        public SoundId CastSound;
        [Tooltip("Played on hits with damage (skills whose hits are explosions use the explosion's sound instead).")]
        public SoundId HitSound;

        [Header("Visuals")]
        [Tooltip("Projectile body, area telegraph disc or aura ring.")]
        public Mesh ProjectileMesh;
        public Material ProjectileMaterial;
        public float ProjectileScale = 0.5f;
        public Mesh ExplosionMesh;
        public Material ExplosionMaterial;
        public float ExplosionDuration = 0.18f;
    }
}
