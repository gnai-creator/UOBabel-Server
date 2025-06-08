using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;

namespace UOContent.Custom.Features.Ironman.Gumps
{

    public class IronmanRankingEntry
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }
        public string SurvivalTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class IronmanRankingGump : Gump
    {
        public IronmanRankingGump(PlayerMobile user, List<IronmanRankingEntry> ranking) : base(0, 0)
        {
            Closable = true;
            Disposable = true;
            Draggable = true;

            int gumpWidth = 450;
            int gumpHeight = 45 + 26 * (ranking.Count + 1);

            AddPage(0);
            AddBackground(0, 0, gumpWidth, gumpHeight, 9270);

            AddLabel(gumpWidth / 2 - 60, 15, 65, "Ironman Ranking");

            int y = 45;

            // Cabeçalho
            AddLabel(24, y, 2208, "Pos.");
            AddLabel(68, y, 2208, "Jogador");
            AddLabel(180, y, 2208, "Score");
            AddLabel(245, y, 2208, "Tempo");
            AddLabel(330, y, 2208, "Status");

            y += 23;

            if (ranking.Count == 0)
            {
                AddLabel(100, y, 33, "Nenhum jogador no ranking ainda!");
                return;
            }

            for (int i = 0; i < ranking.Count; i++)
            {
                var r = ranking[i];

                AddLabel(24, y, 1153, (i + 1).ToString());
                AddLabel(68, y, 1153, r.PlayerName);
                AddLabel(180, y, 1153, r.Score.ToString());
                AddLabel(245, y, 1153, FormatSurvivalTime(r.SurvivalTime));

                if (r.IsActive)
                    AddLabel(330, y, 61, "Vivo");
                else
                    AddLabel(330, y, 33, "Morto");

                y += 22;
            }
        }

        private string FormatSurvivalTime(string ts)
        {
            if (string.IsNullOrEmpty(ts)) return "-";
            // Exemplo esperado: "1d 00:50:46"
            if (ts.Contains("d"))
            {
                var split = ts.Split('d');
                var days = split[0].Trim();
                var time = split[1].Trim();
                if (days == "0")
                    return time;
                return $"{days}d {time}";
            }
            // Fallback:
            return ts.Length > 8 ? ts.Substring(0, 8) : ts;
        }

    }

}