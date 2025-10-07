using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace TitanVT
{
    [BepInPlugin("648f5c3d-8e5e-4563-b5ed-d38b24528485", "Titan: Veiled Tempest","0.2.0")]
    class TitanVT : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony harmony = new Harmony("648f5c3d-8e5e-4563-b5ed-d38b24528485");
            harmony.PatchAll();
            Debug.Log("TitanVeiledTempest Loaded!");
        }
    }
}