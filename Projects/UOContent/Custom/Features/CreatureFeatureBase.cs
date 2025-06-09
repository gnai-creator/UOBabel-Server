using Server;
using Server.Mobiles;

namespace Server.Custom.Features
{
    public abstract class CreatureFeatureBase : ICreatureFeature
    {
        public BaseCreature Owner { get; set; }

        public virtual void Initialize() { }
        public virtual void OnSpeech(SpeechEventArgs e) { }
        public virtual void OnThink() { }
        public virtual void OnDeath() { }
        public virtual void OnCombat(Mobile target) { }
        public virtual void OnInteract(Mobile player) { }
        public virtual void OnCommand(string command, Mobile from) { }
        public virtual void OnIdle() { }
        public virtual void OnFollow(Mobile target) { }
        public virtual void OnEmotionChanged(string newEmotion) { }
        public virtual void OnDespawn() { }
        public virtual void OnSaved() { }
        public virtual void OnLoaded() { }
        public abstract void Serialize(IGenericWriter writer);
        public abstract void Deserialize(IGenericReader reader);
    }
}
