using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Plugin;
[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.Awake))]
public class OfferingBowlAwake {
  static int Spawn = "override_spawn".GetStableHashCode();
  // prefab
  static int SpawnItem = "override_spawn_item".GetStableHashCode();
  // prefab
  static int Amount = "override_amount".GetStableHashCode();
  // int
  static int StartEffect = "override_start_effect".GetStableHashCode();
  // prefab,flags,variant,childTransform|prefab,flags,variant,childTransform|...
  static int SpawnEffect = "override_spawn_effect".GetStableHashCode();
  // prefab,flags,variant,childTransform|prefab,flags,variant,childTransform|...
  static int UseEffect = "override_use_effect".GetStableHashCode();
  // prefab,flags,variant,childTransform|prefab,flags,variant,childTransform|...
  static int Name = "override_name".GetStableHashCode();
  // string
  static int Text = "override_text".GetStableHashCode();
  // string
  static int Delay = "override_delay".GetStableHashCode();
  // float (seconds)
  static int ItemOffset = "override_item_offset".GetStableHashCode();
  // float,float,float (x,z,y)
  static int SpawnOffset = "override_spawn_offset".GetStableHashCode();
  // float (meters)
  static int SpawnMaxY = "override_spawn_max_y".GetStableHashCode();
  // float (meters)
  static int SpawnRadius = "override_spawn_radius".GetStableHashCode();
  // float (meters)
  static int ItemStandPrefix = "override_item_stand_prefix".GetStableHashCode();
  // string
  static int ItemStandRange = "override_item_stand_range".GetStableHashCode();
  // float (meters)

  static void SetSpawn(OfferingBowl obj, ZNetView view) =>
    Helper.Prefab(view, Spawn, value => {
      var drop = value.GetComponent<ItemDrop>();
      obj.m_bossPrefab = null;
      obj.m_itemPrefab = null;
      if (drop)
        obj.m_itemPrefab = drop;
      else
        obj.m_bossPrefab = value;
    });
  static void SetSpawnItem(OfferingBowl obj, ZNetView view) =>
    Helper.Prefab(view, SpawnItem, value => {
      var drop = value.GetComponent<ItemDrop>();
      if (!drop) return;
      obj.m_bossItem = drop;
    });
  static void SetAmount(OfferingBowl obj, ZNetView view) =>
    Helper.Int(view, Amount, value => {
      obj.m_bossItems = value;
      obj.m_useItemStands = value == 0;
    });
  static void SetDelay(OfferingBowl obj, ZNetView view) =>
    Helper.Float(view, Delay, value => obj.m_spawnBossDelay = value);
  static void SetName(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, Name, value => obj.m_name = value);
  static void SetText(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, Text, value => obj.m_useItemText = value);
  static void SetItemStandPrefix(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, ItemStandPrefix, value => {
      obj.m_useItemStands = true;
      obj.m_itemStandPrefix = value;
    });
  static void SetItemStandRange(OfferingBowl obj, ZNetView view) =>
    Helper.Float(view, ItemStandRange, value => {
      obj.m_useItemStands = true;
      obj.m_itemstandMaxRange = value;
    });
  static void SetSpawnOffset(OfferingBowl obj, ZNetView view) =>
    Helper.Float(view, SpawnOffset, value => obj.m_spawnOffset = value);
  static void SetSpawnRadius(OfferingBowl obj, ZNetView view) =>
    Helper.Float(view, SpawnRadius, value => obj.m_spawnBossMaxDistance = value);
  static void SetSpawnMaxY(OfferingBowl obj, ZNetView view) =>
    Helper.Float(view, SpawnMaxY, value => obj.m_spawnBossMaxYDistance = value);
  static void SetItemOffset(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, ItemOffset, value => {
      var split = value.Split(',');
      var pos = obj.m_itemSpawnPoint.localPosition;
      pos.x = Helper.Float(split[0]);
      if (split.Length > 1)
        pos.z = Helper.Float(split[1]);
      if (split.Length > 2)
        pos.y = Helper.Float(split[2]);
      obj.m_itemSpawnPoint.localPosition = pos;
    });
  static void SetStartEffect(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, StartEffect, value => obj.m_spawnBossStartEffects = Helper.ParseEffects(value));
  static void SetSpawnEffect(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, SpawnEffect, value => obj.m_spawnBossDoneffects = Helper.ParseEffects(value));
  static void SetUseEffect(OfferingBowl obj, ZNetView view) =>
    Helper.String(view, UseEffect, value => obj.m_fuelAddedEffects = Helper.ParseEffects(value));
  static void EnsureItemSpawnPoint(OfferingBowl obj) {
    if (obj.m_itemSpawnPoint) return;
    GameObject spawnPoint = new();
    spawnPoint.transform.parent = obj.transform;
    spawnPoint.transform.localPosition = Vector3.zero;
    spawnPoint.transform.localRotation = Quaternion.identity;
    obj.m_itemSpawnPoint = spawnPoint.transform;

  }
  public static void Postfix(OfferingBowl __instance) {
    if (!Configuration.configOfferingBowl.Value) return;
    var view = __instance.GetComponentInParent<ZNetView>();
    if (!view || !view.IsValid()) return;
    EnsureItemSpawnPoint(__instance);
    SetSpawn(__instance, view);
    SetAmount(__instance, view);
    SetSpawnItem(__instance, view);
    SetName(__instance, view);
    SetText(__instance, view);
    SetItemStandPrefix(__instance, view);
    SetItemStandRange(__instance, view);
    SetDelay(__instance, view);
    SetItemOffset(__instance, view);
    SetSpawnRadius(__instance, view);
    SetSpawnMaxY(__instance, view);
    SetSpawnOffset(__instance, view);
    SetStartEffect(__instance, view);
    SetSpawnEffect(__instance, view);
    SetUseEffect(__instance, view);
  }
}


[HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SpawnLocation))]
public class UpdateOfferingBowls {
  static void Postfix(LocationProxy __instance, bool __result) {
    if (!__result || !Configuration.configOfferingBowl.Value) return;
    var offeringBowl = __instance.m_instance?.GetComponentInChildren<OfferingBowl>();
    if (offeringBowl != null) OfferingBowlAwake.Postfix(offeringBowl);
  }
}

public static class OfferingBowlHelper {
  static int Respawn = "override_respawn".GetStableHashCode();
  // float (minutes)
  static int SpawnTime = "spawn_time".GetStableHashCode();
  public static bool CanRespawn(OfferingBowl obj) {
    if (!Configuration.configOfferingBowl.Value) return true;
    var view = obj.GetComponentInParent<ZNetView>();
    var ret = true;
    Helper.Float(view, Respawn, respawn => {
      Helper.Long(view, SpawnTime, spawnTime => {
        var now = ZNet.instance.GetTime();
        var date = new DateTime(spawnTime);
        ret = respawn > 0f && (now - date).TotalMinutes >= respawn;
      });
    });
    return ret;
  }
}

[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.Interact))]
public class OfferingBowlRespawnInteract {
  static bool Prefix(OfferingBowl __instance) => OfferingBowlHelper.CanRespawn(__instance);
}

[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.UseItem))]
public class OfferingBowlRespawnUseItem {
  static bool Prefix(OfferingBowl __instance) => OfferingBowlHelper.CanRespawn(__instance);
}

[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.SpawnBoss))]
public class OfferingBowlSetSpawnTime {
  static int SpawnTime = "spawn_time".GetStableHashCode();
  static void Postfix(OfferingBowl __instance, bool __result) {
    if (!__result) return;
    var view = __instance.GetComponentInParent<ZNetView>();
    if (!view) return;
    view.GetZDO().Set(SpawnTime, ZNet.instance.GetTime().Ticks);
  }
}

[HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.DelayedSpawnBoss))]
public class OfferingBowlSetupSpawn {
  static int MinLevel = "override_minimum_level".GetStableHashCode();
  // int
  static int MaxLevel = "override_maximum_level".GetStableHashCode();
  // int
  static int LevelChance = "override_level_chance".GetStableHashCode();
  // float (percent)
  static int Health = "override_health".GetStableHashCode();
  // float
  static void Setup(BaseAI baseAI, OfferingBowl bowl) {
    if (!Configuration.configOfferingBowl.Value) return;
    var obj = baseAI.GetComponent<Character>();
    if (!obj) return;
    var view = bowl.GetComponentInParent<ZNetView>();
    var levelChance = 10f;
    var minLevel = 1;
    var maxLevel = 1;
    Helper.Float(view, LevelChance, value => levelChance = value);
    Helper.Int(view, MinLevel, value => minLevel = value);
    Helper.Int(view, MaxLevel, value => maxLevel = value);
    var level = Helper.RollLevel(minLevel, maxLevel, levelChance);
    if (level > 1) obj.SetLevel(level);
    Helper.Float(view, Health, obj.SetMaxHealth);
  }
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
    return new CodeMatcher(instructions).MatchForward(false, new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(BaseAI), nameof(BaseAI.SetPatrolPoint))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Call, Transpilers.EmitDelegate(Setup).operand))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0))
      .InstructionEnumeration();
  }
}
