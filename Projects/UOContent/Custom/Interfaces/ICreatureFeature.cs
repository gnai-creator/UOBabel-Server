using Server;
using Server.Mobiles;

namespace Server.Custom.Features
{
    public interface ICreatureFeature
    {
        BaseCreature Owner { get; set; }
        void Initialize();
        void OnSpeech(SpeechEventArgs e);
        void OnThink();
        void OnDeath();
        void OnCombat(Mobile target);
        void OnInteract(Mobile player);
        void OnCommand(string command, Mobile from);
        void OnIdle();
        void OnFollow(Mobile target);
        void OnEmotionChanged(string newEmotion);
        void OnDespawn();
        void OnSaved();
        void OnLoaded();
        void Serialize(IGenericWriter writer);
        void Deserialize(IGenericReader reader);
    }
}
