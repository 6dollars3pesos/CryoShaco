using Aimtec;
using Aimtec.SDK.Events;

namespace CryoShaco
{
    class Program
    {
        static void Main(string[] args)
        {
            GameEvents.GameStart += Game_OnStart;
        }

        private static void Game_OnStart()
        {
            if (ObjectManager.GetLocalPlayer().ChampionName != "Shaco") return;

            var shaco = new CryoShaco();
        }
    }
}
