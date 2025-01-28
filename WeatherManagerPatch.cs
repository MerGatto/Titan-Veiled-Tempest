using Assets.Scripts.Atmospherics;
using Assets.Scripts;
using Assets.Scripts.Objects;
using HarmonyLib;
using JetBrains.Annotations;
using System.Reflection;
using Weather;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using static Assets.Scripts.Util.Defines;

namespace ToxicTitan
{
    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.StopCurrentWeatherEvent))]
    [UsedImplicitly]
    public static class WeatherManager_StopCurrentWeatherEvent_Patch
    {
        public static void Postfix()
        {
            Type type = typeof(PlanetaryAtmosphereSimulation);

            FieldInfo info = type.GetField("_globalGasMix", BindingFlags.NonPublic | BindingFlags.Static);
            var globalGasMix = (GlobalGasMix)info.GetValue(null);

            // Clear LiquidClouds
            info = type.GetField("_liquidClouds", BindingFlags.NonPublic | BindingFlags.Static);
            var liquidClouds = (GlobalGasMix)info.GetValue(null);
            globalGasMix.Add(liquidClouds, AtmosphereHelper.MatterState.All);
            liquidClouds.ClearQuantities(AtmosphereHelper.MatterState.All);

            // Clear IceClouds
            info = type.GetField("_iceClouds", BindingFlags.NonPublic | BindingFlags.Static);
            var iceClouds = (GlobalGasMix)info.GetValue(null);
            globalGasMix.Add(iceClouds, AtmosphereHelper.MatterState.All);
            iceClouds.ClearQuantities(AtmosphereHelper.MatterState.All);
        }
    }

    [HarmonyPatch(typeof(WeatherManager), "HasWorldStartCooldownPastInDays")]
    [UsedImplicitly]
    public static class WeatherManager_HasWorldStartCooldownPastInDays_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (WorldManager.CurrentWorldName == "ToxicTitan")
            {
                __result = WorldManager.DaysPast > 1;
            }
        }
    }

    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.GetNextWeatherEvent))]
    [UsedImplicitly]
    public static class WeatherManager_GetNextWeatherEvent_Patch
    {
        private static bool _firstStorm = true;

        public static void Postfix(ref WeatherEvent __result)
        {
            if (WorldManager.CurrentWorldName == "ToxicTitan" && _firstStorm)
            {
                ConsoleWindow.Print("Setting First Storm to TitanToxicRain");
                _firstStorm = false;
                __result = DataCollection.Get<WeatherEvent>(UnityEngine.Animator.StringToHash("TitanToxicRain"));
            }
        }
    }


}
