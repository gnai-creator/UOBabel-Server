using Server;

namespace Server.Custom.Interfaces
{
    public interface IPlayerFeature
    {
        void OnLogin();
        void OnDeath();
        void Serialize(IGenericWriter writer);
        void Deserialize(IGenericReader reader);
    }

}