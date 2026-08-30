using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterStorm
{
    internal static class StormPositionCoordinates
    {
        internal static Vector3 ToSaveCoordinates(Vector3 runtimePosition)
        {
            FloatingOriginManager origin = FloatingOriginManager.instance;
            return origin != null
                ? origin.ShiftingPosToRealPos(runtimePosition)
                : runtimePosition;
        }

        internal static Vector3 ToRuntimeCoordinates(Vector3 savedPosition)
        {
            FloatingOriginManager origin = FloatingOriginManager.instance;
            return origin != null
                ? origin.RealPosToShiftingPos(savedPosition)
                : savedPosition;
        }
    }

    internal static class StormPositionPersistence
    {
        private static readonly PositionOwner[] Owners = BuildOwners();

        private static PositionOwner[] BuildOwners()
        {
            PositionOwner[] owners =
                new PositionOwner[StormCatalog.Custom.Length];
            for (int i = 0; i < StormCatalog.Custom.Length; i++)
            {
                StormDefinition definition = StormCatalog.Custom[i];
                owners[i] = new PositionOwner(
                    definition.Id,
                    definition.Name,
                    definition.SaveKey);
            }

            return owners;
        }

        internal static void BeginLoadAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].BeginLoad();
            }
        }

        internal static void LoadAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Load();
            }
        }

        internal static void SaveAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Save();
            }
        }

        internal static void ResetAll()
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                Owners[i].Reset();
            }
        }

        internal static void ApplyLoadedPosition(
            CustomStormId id,
            ModStormController controller)
        {
            for (int i = 0; i < Owners.Length; i++)
            {
                if (Owners[i].StormId == id)
                {
                    Owners[i].ApplyLoadedPosition(controller);
                    return;
                }
            }
        }

        internal sealed class PositionOwner
        {
            private readonly CustomStormId stormId;
            private readonly string displayName;
            private readonly string betterStormV2Key;

            private bool loadCompleted;
            private bool hasLoadedPosition;
            private Vector3 loadedPosition;

            internal PositionOwner(
                CustomStormId stormId,
                string displayName,
                string betterStormV2Key)
            {
                this.stormId = stormId;
                this.displayName = displayName;
                this.betterStormV2Key = betterStormV2Key;
            }

            internal CustomStormId StormId
            {
                get { return stormId; }
            }

            internal void BeginLoad()
            {
                loadCompleted = false;
                hasLoadedPosition = false;
                loadedPosition = default(Vector3);
            }

            internal void Save()
            {
                ModStormController controller = ModStormFactory.Find(stormId);
                if (controller == null)
                {
                    return;
                }

                if (GameState.modData == null)
                {
                    GameState.modData = new Dictionary<string, string>();
                }

                Vector3 position = StormPositionCoordinates.ToSaveCoordinates(
                    controller.transform.position);
                SaveData data = new SaveData
                {
                    version = 2,
                    x = position.x,
                    y = position.y,
                    z = position.z
                };
                GameState.modData[betterStormV2Key] = JsonUtility.ToJson(data);
            }

            internal void Load()
            {
                loadCompleted = true;
                hasLoadedPosition = false;

                if (TryLoadKey(
                        betterStormV2Key,
                        out Vector3 position))
                {
                    loadedPosition = position;
                    hasLoadedPosition = true;
                }

                ApplyLoadedPosition(ModStormFactory.Find(stormId));
            }

            internal void ApplyLoadedPosition(ModStormController controller)
            {
                if (!loadCompleted || !hasLoadedPosition || controller == null)
                {
                    return;
                }

                controller.transform.position =
                    StormPositionCoordinates.ToRuntimeCoordinates(loadedPosition);
                controller.EnsureValidInitialPlacement();
            }

            internal void Reset()
            {
                loadCompleted = false;
                hasLoadedPosition = false;
                loadedPosition = default(Vector3);
            }

            private bool TryLoadKey(
                string key,
                out Vector3 position)
            {
                position = default(Vector3);
                if (GameState.modData == null ||
                    !GameState.modData.TryGetValue(key, out string json) ||
                    string.IsNullOrEmpty(json))
                {
                    return false;
                }

                try
                {
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    if (data == null ||
                        data.version != 2 ||
                        !IsFinite(data.x) ||
                        !IsFinite(data.y) ||
                        !IsFinite(data.z))
                    {
                        BetterStormPlugin.Instance?.LogFeatureWarning(
                            "Ignoring invalid " + displayName +
                            " save data in key " + key + ".");
                        return false;
                    }

                    position = new Vector3(data.x, data.y, data.z);
                    return true;
                }
                catch (Exception exception)
                {
                    BetterStormPlugin.Instance?.LogFeatureWarning(
                        "Ignoring invalid " + displayName +
                        " save data in key " + key + ": " +
                        exception.Message);
                    return false;
                }
            }

            private static bool IsFinite(float value)
            {
                return !float.IsNaN(value) && !float.IsInfinity(value);
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public float x;
            public float y;
            public float z;
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadGame")]
    internal static class CustomStormBeginLoadPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            WindOverrideState.RestoreImmediately();
            RuntimeEffectLifecycle.ShutdownTransientEffects();
            StormPositionPersistence.BeginLoadAll();
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
    internal static class CustomStormSavePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            StormPositionPersistence.SaveAll();
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
    internal static class CustomStormLoadPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            StormPositionPersistence.LoadAll();
        }
    }
}
