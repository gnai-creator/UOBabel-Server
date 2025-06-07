using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Commands.Generic;
using Server.Custom.Mobiles;
namespace UOContent.Custom.Commands
{
    public class AtualizaPatreonCommand : BaseCommand
    {
        public static void Configure()
        {
            CommandSystem.Register("atualizarpatreon", AccessLevel.Player, AtualizarPatreon_Command);
        }

        public static void AtualizarPatreon_Command(CommandEventArgs e)
        {
            if (e.Mobile is CustomPlayer player)
            {
                _ = player.AtualizarPatreonAsync(force: true);
                e.Mobile.SendMessage("Atualizando informações do Patreon...");
            }
        }
    }
}