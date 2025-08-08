using dc.en.hero;
using HaxeProxy.Runtime;
using ModCore.Mods;

namespace TripleGold
{
    public class TripleGoldMod(ModInfo info) : ModBase(info)
    {
        private void Hook_beheaded_addMoney(Hook_Beheaded.orig_addMoney orig, Beheaded self, int val, Ref<bool> noStats)
        {
            val *= 3;
            orig(self, val, noStats);
        }

        public override void Initialize()
        {
            Hook_Beheaded.addMoney += Hook_beheaded_addMoney;
        }
    }
}