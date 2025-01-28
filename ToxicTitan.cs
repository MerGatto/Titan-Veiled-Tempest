using HarmonyLib;
using StationeersMods.Interface;

namespace ToxicTitan
{
    [StationeersMod("ToxicTitan","ToxicTitan [StationeersMods]","0.2.4657.21547.1")]
    class ToxicTitan : ModBehaviour
    {
        // private ConfigEntry<bool> configBool;
        public override void OnLoaded(ContentHandler contentHandler)
        {
            Harmony harmony = new Harmony("ToxicTitan");
            harmony.PatchAll();
            UnityEngine.Debug.Log("ToxicTitan Loaded!");
        }
    }
}