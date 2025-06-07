using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server;
using Server.Items;

namespace Server.Custom.Items
{
    public static class ItemParser
    {
        public static Item IdentifyItem(string itemName, int amount)
        {
            Console.WriteLine("[ItemParser] Identifying item: " + itemName + " - " + amount);

            switch (itemName.ToLower())
            {
                case "dinheiro":
                    return new Gold(amount);
                case "ouro":
                    return new Gold(amount);
                case "bardiche":
                    return new Bardiche();
                case "arco":
                    return new Bow();
                case "besta":
                    return new Crossbow();
                case "adaga":
                    return new Dagger();
                case "alabarda":
                    return new Halberd();
                case "katana":
                    return new Katana();
                case "kryss":
                    return new Kryss();
                case "espada":
                    return new Longsword();
                case "machado":
                    return new Axe();
                case "picareta":
                    return new Pickaxe();
                case "simitarra":
                    return new Scimitar();
                case "lança":
                    return new ShortSpear();
                case "foice":
                    return new Scythe();
                case "porrete":
                    return new Club();
                case "marreta":
                    return new Mace();
                case "bordao":
                    return new QuarterStaff();
                case "escudo":
                    return new BronzeShield();
                case "broquel":
                    return new Buckler();
                case "madeira":
                    return new Board();
                case "serra":
                    return new Saw();
                case "tambor":
                    return new Drums();
                case "tamborim":
                    return new Tambourine();
                case "harpa":
                    return new LapHarp();
                case "alaude":
                    return new Lute();
                case "sacola":
                    return new Bag();
                case "bolsa":
                    return new Pouch();
                case "mochila":
                    return new Backpack();
                case "couro":
                    return new Leather();
                case "pao":
                    return new BreadLoaf();
                case "bolo":
                    return new Cake();
                case "cookies":
                    return new Cookies();
                case "pizza":
                    return new CheesePizza();
                case "bandage":
                    return new Bandage(amount);
                case "bandages":
                    return new Bandage(amount);
                case "setas":
                    return new Bolt(amount);
                case "flecha":
                    return new Arrow(amount);
                case "flechas":
                    return new Arrow(amount);
                case "tesoura":
                    return new Scissors();
                default:
                    return null;
           }
        }
    }
}