using Server;

namespace Server.Custom.Interfaces
{
    public interface IPlayerFeature
    {
        void Initialize(Mobile owner);
        void OnLogin();
        void OnDeath();
        void OnKill(Mobile victim, Mobile killer);
        void OnThink();
        void Serialize(IGenericWriter writer);
        void Deserialize(IGenericReader reader);
    }

}