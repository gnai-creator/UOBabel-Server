using Server;

namespace Server.Custom.Features
{
    public interface ICreatureFeature
    {
        void  OnSpeech(SpeechEventArgs e);
        void OnThink();
        void OnDeath();
        void OnCombat(Mobile target);
        void Serialize(IGenericWriter writer);
        void Deserialize(IGenericReader reader);
    }

}