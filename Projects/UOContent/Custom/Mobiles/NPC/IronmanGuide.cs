using System;
using Server;
using Server.Mobiles;
using Server.Custom.Mobiles;
using Server.Custom.Features;
using Server.Custom.AI;
using System.Threading.Tasks;

namespace Server.Custom.NPCs
{
    public class IronmanGuide : CustomCreature
    {
        public override bool ClickTitle => true;

        private DateTime _nextTalkTime;

        public IronmanGuide() : base(AIType.AI_Vendor)
        {
            Name = "Hermes";
            Title = "The Guide";
            Body = 400;
            Hue = 0;
            Blessed = true;

            CantWalk = true;
            Direction = Direction.South;
        }

        private async Task HandleMovementAsync(Mobile m, Point3D oldLocation)
        {
            if (m is CustomPlayer player && DateTime.UtcNow > _nextTalkTime && InRange(player, 8) && !InRange(oldLocation, 8))
            {
                var text = await AITranslator.TranslateTo(player, player.PreferredLanguage, "Ei, você! Quer participar do desafio Ironman? Diga 'ironman' ou 'aceito'!");
                var translated = text.ToString();
                SayTo(player, translated);
                _nextTalkTime = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);
            _ = HandleMovementAsync(m, oldLocation);
        }

        private async Task HandleSpeechAsync(SpeechEventArgs e)
        {
            if (e.Mobile is CustomPlayer player && InRange(e.Mobile, 3))
            {
                string speech = e.Speech.ToLower();

                string speechTranslated = await AITranslator.TranslateTo(player, "pt", speech);

                if (speechTranslated.Contains("ironman") || speechTranslated.Contains("quero ser ironman") || speechTranslated.Contains("aceito"))
                {
                    if (player.Manager.Features.TryGetValue("ironman", out var feature) && feature is IronmanFeature ironman)
                    {
                        if (ironman.IsActive)
                        {
                            var text = await AITranslator.TranslateTo(player, player.PreferredLanguage, "Você já está no modo Ironman. Agora, sobreviva!");
                            var translated = text.ToString();
                            SayTo(player, translated);
                        }
                        else
                        {
                            ironman.IsActive = true;
                            ironman.IronmanStartTime = Core.Now;
                            ironman.IronmanStartRegion = player.Region?.Name ?? "Desconhecida";
                            ironman.IronmanScore = 0;

                            player.SendMessage(33, "[Ironman] Você agora está em modo Ironman!");
                            var text = await AITranslator.TranslateTo(player, player.PreferredLanguage, "Muito bem. Que a morte seja apenas uma lenda para você...");
                            var translated = text.ToString();
                            SayTo(player, translated);
                        }
                    }
                    else
                    {
                        SayTo(player, "Houve um erro ao ativar seu modo Ironman. Avise um administrador.");
                    }

                    e.Handled = true;
                }
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);
            _ = HandleSpeechAsync(e);
        }

        public IronmanGuide(Serial serial) : base(serial) { }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            _nextTalkTime = DateTime.UtcNow;
            reader.ReadInt();
        }
    }
}
