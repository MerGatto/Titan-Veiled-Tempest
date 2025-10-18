using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using HarmonyLib;
using JetBrains.Annotations;
using SimpleSpritePacker;
using Sound;
using StormVolumes;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Weather;

namespace TitanVT
{
    public static class Const
    {
        public const string TitanVTId = "TitanVT";
        public const string TitanToxicRainId = "TitanToxicRain";
    }

    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.StopCurrentWeatherEvent))]
    [UsedImplicitly]
    public static class WeatherManager_StopCurrentWeatherEvent_Patch
    {
        public static void Postfix()
        {
            if (WorldManager.CurrentWorldId == Const.TitanVTId)
            {
                ConsoleWindow.Print("Clearing Liquid Clouds");
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
    }

    [HarmonyPatch(typeof(WeatherManager), "HasWorldStartCooldownPastInDays")]
    public static class WeatherManager_HasWorldStartCooldownPastInDays_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            if (WorldManager.CurrentWorldId == Const.TitanVTId)
            {
                __result = WorldManager.DaysPast > 1;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.GetNextWeatherEvent))]
    [UsedImplicitly]
    public static class WeatherManager_GetNextWeatherEvent_Patch
    {
        public static void Postfix(ref WeatherEvent __result)
        {
            if (WorldManager.CurrentWorldId == Const.TitanVTId && WorldManager.DaysPast < 5)
            {
                Debug.Log("Forcing Storm to TitanToxicRain in first 5 days");
                __result = DataCollection.Get<WeatherEvent>(Animator.StringToHash("TitanToxicRain"));
            }
        }
    }

    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.GetStormDirection))]
    [UsedImplicitly]
    public static class WeatherManager_GetStormDirection_Patch
    {
        public static void Postfix(ref Vector3 __result)
        {
            if (WeatherManager.CurrentWeatherEvent.IdHash == Animator.StringToHash(Const.TitanToxicRainId))
            {
                Debug.Log("Forcing Storm direction down for TitanToxicRain");
                __result = new Vector3(__result.x, -0.6f, __result.z ).normalized;
            }
        }
    }

    [HarmonyPatch(typeof(EnvironmentalAudioHandler), "SpawnEmittersInShells")]
    [UsedImplicitly]
    public static class EnvironmentalAudioHandler_SpawnEmittersInShells_Patch
    {

        private static Dictionary<int, HashSet<int>> _onionMapping;
        private static int _stormSize = 11;
        private static int _gridSize = 2;
        private static int _effectZoneTotalVolume;
        private static Grid3 _gridOffset;

        static EnvironmentalAudioHandler_SpawnEmittersInShells_Patch()
        {
            _onionMapping = new Dictionary<int, HashSet<int>>();
            _effectZoneTotalVolume = _stormSize * _stormSize * _stormSize;
            _gridOffset = new Grid3(_stormSize / 2);
            for (int i = 0; i < _effectZoneTotalVolume; i++)
            {
                Grid3 right = IndexToLocal(i, _stormSize, _gridSize);
                Grid3 grid = (_gridOffset * _gridSize - right) / _gridSize;
                int key = Mathf.Max(new int[]
                {
                    Mathf.Abs(grid.x),
                    Mathf.Abs(grid.y),
                    Mathf.Abs(grid.z)
                });
                if (!_onionMapping.ContainsKey(key))
                {
                    _onionMapping.Add(key, new HashSet<int>());
                }
                _onionMapping[key].Add(i);
            }
        }

        private static Grid3 IndexToLocal(int index, int stormSize, int gridSize)
        {
            return new Grid3(index % stormSize, index / stormSize % stormSize, index / (stormSize * stormSize)) * gridSize;
        }

        private static Vector3 IndexToWorld(int index)
        {
            return LocalToWorld(IndexToLocal(index, _stormSize, _gridSize));
        }

        private static Vector3 LocalToWorld(Grid3 local)
        {
            Vector3 worldPositionGridCentre = (InventoryManager.WorldPosition - Vector3.up * 0.5f).GridCenter(2f, 0f);
            return worldPositionGridCentre + (local - _gridOffset * _gridSize).ToVector3Raw();
        }

        public static void Postfix(ref EnvironmentalAudioHandler __instance)
        {
            if (WeatherManager.CurrentWeatherEvent.IdHash == Animator.StringToHash(Const.TitanToxicRainId))
            {
                List<int> _workingList = new List<int>();
                for (int i = 0; i < __instance.ShellDatas.Length; i++)
                {
                    EnvironmentalAudioHandler.ShellData shellData = __instance.ShellDatas[i];
                    if (shellData.PlayShell && shellData.EmitterData.Count < shellData.MaxCount)
                    {
                        HashSet<int> hashSet = _onionMapping[i + 1];
                        _workingList.Clear();
                        foreach (int num in hashSet)
                        {
                            _workingList.Add(num);
                        }
                        if (_workingList.Count != 0)
                        {
                            for (int j = shellData.EmitterData.Count; j <= shellData.MaxCount; j++)
                            {
                                int index = _workingList[UnityEngine.Random.Range(0, _workingList.Count)];
                                int shellSound = WeatherManager.CurrentWeatherEvent.GetShellSound(i);
                                Vector3 vector = IndexToWorld(index);
                                Singleton<AudioManager>.Instance.PlayAudioClipsData(shellSound, vector, 1f, 1f);
                                shellData.EmitterData.Add(new EnvironmentalAudioHandler.EmitterDatum(vector, GameManager.GameTime + shellData.CoolDownTime * UnityEngine.Random.Range(0.85f, 1.2f)));
                            }
                        }
                    }
                }
            }
        }
    }




}
