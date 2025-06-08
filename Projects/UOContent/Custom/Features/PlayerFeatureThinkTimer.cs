using System;
using Server;
using Server.Custom.Mobiles;

namespace Server.Custom.Features
{
    public class PlayerFeatureThinkTimer : Timer
    {
        private readonly CustomPlayer _player;
        private readonly PlayerManager _manager;

        public PlayerFeatureThinkTimer(CustomPlayer player, PlayerManager manager)
            : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(60.0))
        {
            _player = player;
            _manager = manager;
        }

        protected override void OnTick()
        {
            if (_player.Deleted || !_player.Alive || _player.NetState == null)
            {
                Stop();
                return;
            }

            if (_manager.Features.TryGetValue("ironman", out var feature))
            {
                feature.OnThink();
            }
        }
    }
}
