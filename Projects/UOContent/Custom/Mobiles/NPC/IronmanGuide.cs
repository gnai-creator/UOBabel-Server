using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Server;
using Server.Mobiles;
using Server.Custom.Mobiles;
using Server.Custom.Features;
using Server.Custom.AI;
using System.Threading.Tasks;
using Server.Gumps;
using UOContent.Custom.Features.Ironman.Gumps;

namespace Server.Custom.NPCs
{
    public class IronmanGuide : BaseAICreature
    {
        public override bool ClickTitle => true;

        private DateTime _nextTalkTime;

        [Constructible]
        public IronmanGuide() : base()
        {
            Name = "Hermes";
            Title = "The Guide";
            Body = 400;
            Hue = 0;
            Blessed = true;

            CantWalk = true;
            Direction = Direction.South;
        }

        private async Task<List<IronmanRankingEntry>> GetRankingAsync()
        {
            using var http = new HttpClient();
            var res = await http.GetStringAsync("https://uobabel.com/api/ironman-ranking");
            var list = JsonSerializer.Deserialize<List<IronmanRankingEntry>>(res);
            return list ?? new List<IronmanRankingEntry>();
        }

        private async Task HandleMovementAsync(Mobile m, Point3D oldLocation)
        {
            if (m is CustomPlayer player && DateTime.UtcNow > _nextTalkTime && InRange(player, 8) && !InRange(oldLocation, 8))
            {
                await FalarComEmocao("Ei, você! Quer participar do desafio Ironman? Diga 'ironman' ou 'aceito'!", "afeto", player.PreferredLanguage);
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

                string speechTranslated = await AITranslator.TranslateTo(player, player.PreferredLanguage, speech);

                if (speechTranslated.Contains("ironman") || speechTranslated.Contains("quero ser ironman") || speechTranslated.Contains("aceito"))
                {
                    if (player.Manager.Features.TryGetValue("ironman", out var feature) && feature is IronmanFeature ironman)
                    {
                        if (ironman.IsActive)
                        {
                            await FalarComEmocao("Você já está no modo Ironman. Agora, sobreviva!", "afeto", player.PreferredLanguage);
                        }
                        else
                        {
                            ironman.StartRun();
                            player.SendMessage(33, "[Ironman] Você agora está em modo Ironman!");
                            await FalarComEmocao("Você agora está em modo Ironman! Agora, sobreviva!", "afeto", player.PreferredLanguage);
                        }

                    }
                    else
                    {
                        SayTo(player, "Houve um erro ao ativar seu modo Ironman. Avise um administrador.");
                    }

                    e.Handled = true;
                }
                if (speechTranslated.Contains("ranking") || speechTranslated.Contains("top") || speechTranslated.Contains("melhores"))
                {
                    ((Mobile)player).CloseGump<IronmanRankingGump>(); // fecha se já estiver aberto
                    var ranking = await GetRankingAsync();
                    ((Mobile)player).SendGump(new IronmanRankingGump((PlayerMobile)player, ranking));
                    await FalarComEmocao("Aqui está o ranking dos maiores sobreviventes do Ironman!", "empolgado", player.PreferredLanguage);
                    e.Handled = true;
                    return;
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
