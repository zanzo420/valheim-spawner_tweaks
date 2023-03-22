using HarmonyLib;

namespace SpawnerTweaks;

[HarmonyPatch(typeof(Beehive))]
public class BeehivePatches
{
  static int MaxAmount = "override_maximum_amount".GetStableHashCode();
  // int
  static int Spawn = "override_spawn".GetStableHashCode();
  // int (hash)
  static int Biome = "override_biome".GetStableHashCode();
  // int (biome)
  static int Speed = "override_speed".GetStableHashCode();
  // float
  static int MaxCover = "override_maximum_cover".GetStableHashCode();
  // float
  static int SpawnCondition = "override_spawn_condition".GetStableHashCode();
  // flag (1 = day only)
  static int CoverOffset = "override_cover_offset".GetStableHashCode();
  // float,float,float (x,z,y)
  static int SpawnOffset = "override_spawn_offset".GetStableHashCode();
  // float,float,float (x,z,y)
  static int SpawnEffect = "override_spawn_effect".GetStableHashCode();
  // prefab,flags,variant,childTransform|prefab,flags,variant,childTransform|...

  [HarmonyPatch(nameof(Beehive.Awake)), HarmonyPostfix]
  static void Setup(Beehive __instance)
  {
    if (!Configuration.configBeehive.Value) return;
    var obj = __instance;
    var view = obj.m_nview;
    if (!view || !view.IsValid()) return;
    if (!__instance.m_beeEffect) __instance.m_beeEffect = new();
    Helper.Int(view, MaxAmount, value => obj.m_maxHoney = value);
    Helper.Int(view, Biome, value => obj.m_biome = (Heightmap.Biome)value);
    Helper.Int(view, SpawnCondition, value => obj.m_effectOnlyInDaylight = value == 1);
    Helper.Item(view, Spawn, value => obj.m_honeyItem = value);
    Helper.Float(view, Speed, value => obj.m_secPerUnit = value);
    Helper.Float(view, MaxCover, value => obj.m_maxCover = value);
    Helper.Offset(view, CoverOffset, obj.m_coverPoint, value => obj.m_coverPoint = value);
    Helper.Offset(view, SpawnOffset, obj.m_spawnPoint, value => obj.m_spawnPoint = value);
    Helper.String(view, SpawnEffect, value => obj.m_spawnEffect = Helper.ParseEffects(value));
  }
}
