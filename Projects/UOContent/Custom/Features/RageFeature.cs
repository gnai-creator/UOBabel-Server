using System;
using Server;
using Server.Mobiles;
using Server.Custom.Features;

namespace Server.Custom.Features
{
    public class RageFeature : CreatureFeatureBase
    {
        public bool IsEnraged { get; private set; } = false;
        public int RageLevel { get; private set; } = 0;
        public DateTime LastCombatTime { get; private set; } = DateTime.UtcNow;

        private const int MaxRage = 100;
        private const int RageDecayPerSecond = 5;
        private const int RageGainPerCombat = 15;
        private const int RageThreshold = 50;
        private int? _originalHue = null;
        private DateTime? _rageStart = null;
        private const int RageHue = 33;

        // Efeitos
        private const int RageSoundId = 0x2A3;     // Som de rage
        private const int RageParticleId = 0x36BD; // Partícula de energia
        private const int RageParticleHue = 33;

        private DateTime _nextLoopParticle = DateTime.MinValue;

        // Buffs
        private const double RageDamageMultiplier = 1.5;  // 50% mais dano
        private const double RageSpeedMultiplier = 1.3;   // 30% mais rápido
        private bool _buffsApplied = false;

        // Salva os valores originais para garantir que nunca empilha!
        private int? _baseDamageMin = null;
        private int? _baseDamageMax = null;
        private double? _baseActiveSpeed = null;
        private double? _basePassiveSpeed = null;
        private double? _baseCurrentSpeed = null;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ForceRage
        {
            get => IsEnraged;
            set
            {
                if (value)
                    TriggerEnrage();
                else
                    CalmDown();
            }
        }


        public void TriggerEnrage()
        {
            if (!IsEnraged)
            {
                RageLevel = MaxRage; // ou um valor mínimo para garantir ativação
                EnterRage();
            }
        }


        public override void OnThink()
        {
            if (IsEnraged)
            {
                // Loop de partícula: a cada 1,5s, faz um efeito
                if (DateTime.UtcNow > _nextLoopParticle)
                {
                    ShowLoopParticles();
                    _nextLoopParticle = DateTime.UtcNow + TimeSpan.FromSeconds(1.5);
                }

                // Rage decai ao longo do tempo sem combate
                TimeSpan timeSinceLastCombat = DateTime.UtcNow - LastCombatTime;
                int decay = (int)(timeSinceLastCombat.TotalSeconds * RageDecayPerSecond);

                if (decay > 0)
                {
                    RageLevel = Math.Max(0, RageLevel - decay);
                    LastCombatTime = DateTime.UtcNow;
                }

                if (RageLevel <= 0)
                    CalmDown();
            }
        }

        public override void OnCombat(Mobile target)
        {
            LastCombatTime = DateTime.UtcNow;
            RageLevel = Math.Min(MaxRage, RageLevel + RageGainPerCombat);

            if (!IsEnraged && RageLevel >= RageThreshold)
                EnterRage();

            if (IsEnraged && target != null && target.Alive)
                Owner?.PublicOverheadMessage(Server.MessageType.Emote, RageHue, false, "*ATACA ENFURECIDO!*");
        }

        public override void OnDeath() => CalmDown();

        public override void OnEmotionChanged(string newEmotion)
        {
            if (newEmotion == "raiva")
                EnterRage();
        }

        private void EnterRage()
        {
            if (IsEnraged) // Já está em rage, não buffa de novo!
                return;

            IsEnraged = true;
            _rageStart = DateTime.UtcNow;
            _nextLoopParticle = DateTime.UtcNow;

            if (Owner is BaseCreature bc)
            {
                if (_originalHue == null)
                    _originalHue = bc.Hue;
                bc.Hue = RageHue;
                bc.PlaySound(RageSoundId);
                bc.FixedParticles(RageParticleId, 10, 30, 5032, RageParticleHue, 0, EffectLayer.Waist);

                ApplyRageBuffs(bc);
            }

            // Owner?.PublicOverheadMessage(Server.MessageType.Regular, RageHue, false, "*ENTRA EM FÚRIA!*");
        }

        private void CalmDown()
        {
            if (!IsEnraged)
                return;

            if (Owner is BaseCreature bc)
            {
                if (_originalHue != null)
                    bc.Hue = _originalHue.Value;
                RemoveRageBuffs(bc);
            }
            IsEnraged = false;
            RageLevel = 0;
            _rageStart = null;
            _nextLoopParticle = DateTime.MinValue;
            Owner?.PublicOverheadMessage(Server.MessageType.Regular, 0, false, "*Volta ao normal*");
        }

        private void ApplyRageBuffs(BaseCreature bc)
        {
            if (_buffsApplied)
                return;
            _buffsApplied = true;

            // Salva os valores originais para garantir que nunca empilha!
            if (_baseDamageMin == null) _baseDamageMin = bc.DamageMin;
            if (_baseDamageMax == null) _baseDamageMax = bc.DamageMax;
            if (_baseActiveSpeed == null) _baseActiveSpeed = bc.ActiveSpeed;
            if (_basePassiveSpeed == null) _basePassiveSpeed = bc.PassiveSpeed;
            if (_baseCurrentSpeed == null) _baseCurrentSpeed = bc.CurrentSpeed;

            bc.DamageMin = (int)Math.Round(_baseDamageMin.Value * RageDamageMultiplier);
            bc.DamageMax = (int)Math.Round(_baseDamageMax.Value * RageDamageMultiplier);

            bc.ActiveSpeed = _baseActiveSpeed.Value / RageSpeedMultiplier;
            bc.PassiveSpeed = _basePassiveSpeed.Value / RageSpeedMultiplier;
            bc.CurrentSpeed = _baseCurrentSpeed.Value / RageSpeedMultiplier;
        }

        private void RemoveRageBuffs(BaseCreature bc)
        {
            if (!_buffsApplied)
                return;
            _buffsApplied = false;

            // Restaura os valores originais, nunca empilha!
            if (_baseDamageMin != null) bc.DamageMin = _baseDamageMin.Value;
            if (_baseDamageMax != null) bc.DamageMax = _baseDamageMax.Value;
            if (_baseActiveSpeed != null) bc.ActiveSpeed = _baseActiveSpeed.Value;
            if (_basePassiveSpeed != null) bc.PassiveSpeed = _basePassiveSpeed.Value;
            if (_baseCurrentSpeed != null) bc.CurrentSpeed = _baseCurrentSpeed.Value;

            // Limpa para não reusar caso mude stats base
            _baseDamageMin = null;
            _baseDamageMax = null;
            _baseActiveSpeed = null;
            _basePassiveSpeed = null;
            _baseCurrentSpeed = null;
        }

        // Efeito de partícula recorrente enquanto em rage
        private void ShowLoopParticles()
        {
            if (Owner is BaseCreature bc)
            {
                bc.FixedParticles(0x374A, 1, 15, 5032, RageParticleHue, 0, EffectLayer.Waist);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.Write(0);
            writer.Write(IsEnraged);
            writer.Write(RageLevel);
            writer.Write(LastCombatTime);
            writer.Write(_originalHue ?? -1);
            writer.Write(_rageStart ?? DateTime.MinValue);
            writer.Write(_nextLoopParticle);

            writer.Write(_buffsApplied);
            writer.Write(_baseDamageMin ?? -1);
            writer.Write(_baseDamageMax ?? -1);
            writer.Write(_baseActiveSpeed ?? -1.0);
            writer.Write(_basePassiveSpeed ?? -1.0);
            writer.Write(_baseCurrentSpeed ?? -1.0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    IsEnraged = reader.ReadBool();
                    RageLevel = reader.ReadInt();
                    LastCombatTime = reader.ReadDateTime();
                    int origHue = reader.ReadInt();
                    _originalHue = (origHue == -1) ? null : (int?)origHue;
                    var rageStartRaw = reader.ReadDateTime();
                    _rageStart = (rageStartRaw == DateTime.MinValue) ? null : (DateTime?)rageStartRaw;
                    _nextLoopParticle = reader.ReadDateTime();

                    _buffsApplied = reader.ReadBool();
                    int baseMin = reader.ReadInt();
                    int baseMax = reader.ReadInt();
                    double baseActive = reader.ReadDouble();
                    double basePassive = reader.ReadDouble();
                    double baseCurrent = reader.ReadDouble();

                    _baseDamageMin = (baseMin == -1) ? null : (int?)baseMin;
                    _baseDamageMax = (baseMax == -1) ? null : (int?)baseMax;
                    _baseActiveSpeed = (baseActive == -1.0) ? null : (double?)baseActive;
                    _basePassiveSpeed = (basePassive == -1.0) ? null : (double?)basePassive;
                    _baseCurrentSpeed = (baseCurrent == -1.0) ? null : (double?)baseCurrent;
                    break;
            }
        }
    }
}
