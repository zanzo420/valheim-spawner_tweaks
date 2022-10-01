using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SpawnerTweaks;

[HarmonyPatch(typeof(SpawnArea), nameof(SpawnArea.Awake))]
public class SpawnAreaAwake {
  static int SpawnEffect = "override_spawn_effect".GetStableHashCode();
  // prefab,flags,variant,childTransform|prefab,flags,variant,childTransform|...
  static int Spawn = "override_spawn".GetStableHashCode();
  // prefab,weight,minLevel,maxLevel|prefab,weight,minLevel,maxLevel|...
  static int Respawn = "override_respawn".GetStableHashCode();
  // float (seconds)
  static int MaxNear = "override_max_near".GetStableHashCode();
  // int
  static int MaxTotal = "override_max_total".GetStableHashCode();
  // int
  static int LevelChance = "override_level_chance".GetStableHashCode();
  // float (percent)
  static int TriggerDistance = "override_trigger_distance".GetStableHashCode();
  // float (meters)
  static int SpawnRadius = "override_spawn_radius".GetStableHashCode();
  // float (meters)
  static int NearRadius = "override_near_radius".GetStableHashCode();
  // float (meters)
  static int FarRadius = "override_far_radius".GetStableHashCode();
  // float (meters)
  static int SpawnCondition = "override_spawn_condition".GetStableHashCode();
  // flag (1 = day only, 2 = night only, 4 = ground only)

  static void SetLevelChance(SpawnArea obj) =>
    Helper.Float(obj.m_nview, LevelChance, value => obj.m_levelupChance = value);
  static void SetSpawnRadius(SpawnArea obj) =>
    Helper.Float(obj.m_nview, SpawnRadius, value => obj.m_spawnRadius = value);
  static void SetNearRadius(SpawnArea obj) =>
    Helper.Float(obj.m_nview, NearRadius, value => obj.m_nearRadius = value);
  static void SetFarRadius(SpawnArea obj) =>
    Helper.Float(obj.m_nview, FarRadius, value => obj.m_farRadius = value);
  static void SetTriggerDistance(SpawnArea obj) =>
    Helper.Float(obj.m_nview, TriggerDistance, value => obj.m_triggerDistance = value);
  static void SetMaxNear(SpawnArea obj) =>
    Helper.Int(obj.m_nview, MaxNear, value => obj.m_maxNear = value);
  static void SetMaxTotal(SpawnArea obj) =>
    Helper.Int(obj.m_nview, MaxTotal, value => obj.m_maxTotal = value);
  static void SetRespawn(SpawnArea obj) =>
    Helper.Float(obj.m_nview, Respawn, value => obj.m_spawnIntervalSec = value);
  static void SetSpawnEffect(SpawnArea obj) =>
    Helper.String(obj.m_nview, SpawnEffect, value => obj.m_spawnEffects = Helper.ParseEffects(value));
  static void SetSpawn(SpawnArea obj) =>
    Helper.String(obj.m_nview, Spawn, value => obj.m_prefabs = Helper.ParseSpawnsData(value));
  static void SetSpawnCondition(SpawnArea obj) =>
    Helper.Int(obj.m_nview, Spawn, value => obj.m_onGroundOnly = (value & 4) > 0);
  static void Postfix(SpawnArea __instance) {
    if (!Configuration.configSpawnArea.Value) return;
    if (!__instance.m_nview || !__instance.m_nview.IsValid()) return;
    SetLevelChance(__instance);
    SetSpawnRadius(__instance);
    SetNearRadius(__instance);
    SetFarRadius(__instance);
    SetTriggerDistance(__instance);
    SetMaxNear(__instance);
    SetMaxTotal(__instance);
    SetRespawn(__instance);
    SetSpawnEffect(__instance);
    SetSpawn(__instance);
    SetSpawnCondition(__instance);
  }
}

[HarmonyPatch(typeof(SpawnArea))]
public class SpawnAreaTweaks {
  static int Faction = "override_faction".GetStableHashCode();
  // string
  static int Spawn = "override_spawn".GetStableHashCode();
  // prefab,weight,minLevel,maxLevel|prefab,weight,minLevel,maxLevel|...
  private static float? SpawnHealth = null;
  private static string? SpawnFaction = null;
  [HarmonyPatch(nameof(SpawnArea.SelectWeightedPrefab)), HarmonyPostfix]
  static void GetSpawnedData(SpawnArea __instance, SpawnArea.SpawnData __result) {
    SpawnHealth = null;
    SpawnFaction = null;
    Helper.String(__instance.m_nview, Spawn, value => {
      var index = __instance.m_prefabs.IndexOf(__result);
      var split = value.Split('|')[index].Split(',');
      if (split.Length > 4)
        SpawnFaction = split[4];
      if (split.Length > 5)
        SpawnHealth = Helper.Float(split[5], 0f);
    });
  }

  static int SpawnCondition = "override_spawn_condition".GetStableHashCode();
  // string
  [HarmonyPatch(nameof(SpawnArea.SpawnOne)), HarmonyPrefix]
  static bool CheckTime(SpawnArea __instance) {
    if (!Configuration.configSpawnArea.Value) return true;
    var value = __instance.m_nview.GetZDO().GetInt(SpawnCondition, -1);
    if (value < 0) return true;
    if ((value & 1) > 0 && EnvMan.instance.IsNight()) return false;
    if ((value & 2) > 0 && EnvMan.instance.IsDay()) return false;
    return true;
  }


  private static Vector3 GetCenterPoint(Character character, GameObject obj) {
    if (SpawnHealth.HasValue)
      character.SetMaxHealth(SpawnHealth.Value);
    if (Enum.TryParse<Character.Faction>(SpawnFaction, true, out var faction)) {
      character.m_faction = faction;
      character.m_nview.GetZDO().Set(Faction, SpawnFaction);
    }
    return character?.GetCenterPoint() ?? obj.transform.position;
  }

  [HarmonyPatch(nameof(SpawnArea.SpawnOne)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> FixCenterPoint(IEnumerable<CodeInstruction> instructions) {
    return new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Character), nameof(Character.GetCenterPoint))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 4))
      .Set(OpCodes.Call, Transpilers.EmitDelegate(GetCenterPoint).operand).InstructionEnumeration();
  }
}
