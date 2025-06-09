using System;
using Server.Gumps;
using Server.Mobiles;
using Server.Custom.Companions;
using Server.Custom.Mobiles;

namespace Server.Custom.Features.CompanionFeatures.Gumps
{
    public class CompanionStatusGump : Gump
    {
        private readonly PlayerMobile _viewer;
        private readonly CustomCreature _companion;
        private readonly CompanionFeature _feature;

        public CompanionStatusGump(PlayerMobile viewer, CustomCreature companion, CompanionFeature feature) : base(150, 100)
        {
            _viewer = viewer;
            _companion = companion;
            _feature = feature;

            Closable = true;
            Disposable = true;
            Draggable = true;

            AddPage(0);

            int width = 250;
            int height = 150 + (_feature.Skills?.Count ?? 0) * 20;

            AddBackground(0, 0, width, height, 0x13BE);
            AddAlphaRegion(0, 0, width, height);

            int y = 20;
            AddLabel(20, y, 1153, _companion.Name); y += 20;
            AddLabel(20, y, 1153, $"N\u00edvel: {_feature.Level}"); y += 20;
            AddLabel(20, y, 1153, $"XP: {_feature.Experience}/{_feature.Level * 100}"); y += 20;
            AddLabel(20, y, 1153, $"Felicidade: {_feature.Happiness}%"); y += 20;
            AddLabel(20, y, 1153, $"Humor: {_feature.Mood}"); y += 20;

            if (_feature.Skills != null && _feature.Skills.Count > 0)
            {
                AddLabel(20, y, 1153, "Skills:");
                y += 20;

                foreach (var kv in _feature.Skills)
                {
                    AddLabel(40, y, 1153, $"{kv.Key}: {kv.Value}");
                    y += 20;
                }
            }
        }
    }
}