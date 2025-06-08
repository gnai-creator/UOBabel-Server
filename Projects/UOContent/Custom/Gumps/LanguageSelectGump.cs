using Server.Network;
using Server.Mobiles;
using Server.Gumps;
using Server.Custom.AI;
using System.Threading.Tasks;
using Server.Custom.Mobiles;

namespace Server.Custom.Gumps
{
    public class BaseLanguageGump : Gump
    {
        public BaseLanguageGump() : base(100, 100)
        {
            Closable = false;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(0, 0, 350, 170, 9270);
            AddLabel(40, 15, 1152, "Escolha seu idioma / Choose your language:"); // de 60 para 40

            AddButton(30, 60, 4005, 4007, 1, GumpButtonType.Reply, 0); // pt
            AddLabel(65, 60, 1152, "Português");

            AddButton(30, 90, 4005, 4007, 2, GumpButtonType.Reply, 0); // en
            AddLabel(65, 90, 1152, "English");

            AddButton(30, 120, 4005, 4007, 3, GumpButtonType.Reply, 0); // es
            AddLabel(65, 120, 1152, "Español");

            AddButton(215, 60, 4005, 4007, 4, GumpButtonType.Reply, 0); // fr
            AddLabel(250, 60, 1152, "Français");

            AddButton(215, 90, 4005, 4007, 5, GumpButtonType.Reply, 0); // ru
            AddLabel(250, 90, 1152, "Русский");
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            string lang = null;

            switch (info.ButtonID)
            {
                case 1: lang = "PTB"; break;
                case 2: lang = "ENU"; break;
                case 3: lang = "ESN"; break;
                case 4: lang = "FRA"; break;
                case 5: lang = "RUS"; break;
            }

            if (lang != null)
            {
                Confirm(state.Mobile, lang);
            }
            else
            {
                Refuse(state.Mobile);
            }
        }

        // Pode sobrescrever!
        public virtual async Task Confirm(Mobile from, string lang)
        {
            if (from == null) return;
            if (from is CustomPlayer player)
            {
                player.PreferredLanguage = lang;
                string traducao = await AITranslator.TranslateAsync("Idioma configurado com sucesso!", "pt", lang);
                player.SendMessage(traducao);
            }
        }

        public virtual void Refuse(Mobile from)
        {
            if (from == null) return;
            from.SendGump(new BaseLanguageGump());
        }
    }

    // Se quiser, crie um Gump derivado com lógica extra:
    public class LanguageSelectGump : BaseLanguageGump
    {
        public override async Task Confirm(Mobile from, string lang)
        {
            await base.Confirm(from, lang); // salva normalmente
            // Você pode adicionar lógica custom aqui, se quiser
            from.SendMessage("Idioma configurado com sucesso!");
        }
    }
}
