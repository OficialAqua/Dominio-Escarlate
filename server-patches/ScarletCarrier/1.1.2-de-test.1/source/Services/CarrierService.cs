using System;
using System.Collections.Generic;
using ProjectM;
using ScarletCore;
using ScarletCore.Data;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using ScarletCarrier.Models;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace ScarletCarrier.Services;

internal static class CarrierService {
  private const int SpawnInitializationDelayFrames = 3;
  private const int SpawnInitializationAttempts = 5;
  private static readonly Dictionary<ulong, Carrier> Carriers = [];
  private static readonly HashSet<ulong> PendingSpawns = [];
  public static readonly PrefabGUID[] AppearancePrefabs = [
    new(-450600397),  // Bomber
    new(2142021685),  // Alchemist
    new(40217214),    // Lurker
    new(-1099047820), // Night Maiden
    new(-1108748448), // Viper
    new(-274383877),  // Striker
    new(-383158562),  // Lightweaver
    new(1649578802),  // Paladin
    new(-1213645419), // Sentry Officer
    new(565869317),   // Tractor Beamer
    new(-1773935659), // Militia Veteran
    new(1502148822),  // Exsanguinator
    new(-924080115),  // Tazer
    new(-1788957652), // Nun
    new(-444945115),  // Sister
    new(1218339832),  // Cleric
    new(-823557242),  // Devoted
    new(-442412464),  // Slave Master
    new(-1416355128), // Ruffian
    new(-1192403515), // Villager Female
    new(-2085282780), // Villager Male
    new(-1897484769), // Ace Incinerator
  ];

  public static readonly string[] AppearanceNames = [
    "Bomber",
    "Alchemist",
    "Lurker",
    "Night Maiden",
    "Viper",
    "Striker",
    "Lightweaver",
    "Paladin",
    "Sentry Officer",
    "Tractor Beamer",
    "Militia Veteran",
    "Exsanguinator",
    "Tazer",
    "Nun",
    "Sister",
    "Cleric",
    "Devoted",
    "Slave Master",
    "Ruffian",
    "Villager (Female)",
    "Villager (Male)",
    "~*~~Ace Incinerator~".Format(["green", RichTextFormatter.HighlightColor]),
  ];

  public const string CustomAppearances = "CustomAppearances";

  public static void Initialize() {
    var query = GameSystems.EntityManager.CreateEntityQuery(new EntityQueryDesc {
      All = new[] { ComponentType.ReadOnly<ServantData>() },
      Options = EntityQueryOptions.IncludeDisabled
    }).ToEntityArray(Allocator.Temp);

    foreach (var servant in query) {
      if (!servant.Exists()) continue;
      if (!servant.Has<ServantData>() || !servant.Has<NameableInteractable>() || !servant.Has<EntityOwner>()) continue;
      if (servant.Read<NameableInteractable>().Name.Value != Carrier.Id) continue;
      var owner = servant.Read<EntityOwner>().Owner;

      if (!owner.Exists()) continue;

      var player = owner.GetPlayerData();

      if (player == null) continue;

      var coffin = servant.Has<ServantConnectedCoffin>()
        ? servant.Read<ServantConnectedCoffin>().CoffinEntity._Entity
        : Entity.Null;

      if (!coffin.Exists()) {
        Log.Info($"Found orphaned servant for player {player.Name} ({player.PlatformId}), recreating coffin...");

        // Create carrier with orphaned servant and recreate coffin
        var carrier = new Carrier(Entity.Null, servant, player);
        carrier.RecreateCoffin();

        Carriers[player.PlatformId] = carrier;
        carrier.Hide();
        continue;
      }

      var validCarrier = new Carrier(coffin, servant, player);

      Carriers[player.PlatformId] = validCarrier;

      validCarrier.Hide();
    }
  }

  public static void Spawn(ulong platformId) {
    var playerData = platformId.GetPlayerData();
    if (playerData == null) {
      Log.Error($"Failed to find player data for platform ID {platformId}.");
      return;
    }

    if (PendingSpawns.Contains(platformId)) {
      Log.Info($"Carrier initialization is already pending for player {playerData.Name} ({platformId}).");
      return;
    }

    Carrier carrier = null;
    var createdForThisSpawn = false;

    try {
      if (!Carriers.TryGetValue(platformId, out carrier)) {
        carrier = CreateCarrier(playerData);
        createdForThisSpawn = true;
      } else {
        var servantExists = carrier.ServantEntity.Exists();
        var coffinExists = carrier.CoffinEntity.Exists();

        if (!servantExists && !coffinExists) {
          Log.Info($"Both servant and coffin are missing for player {playerData.Name}; creating a replacement carrier.");
          Carriers.Remove(platformId);
          carrier = CreateCarrier(playerData);
          createdForThisSpawn = true;
        } else if (servantExists && !coffinExists) {
          Log.Info($"Coffin is missing for player {playerData.Name}; recreating it.");
          carrier.RecreateCoffin();
        } else if (!servantExists && coffinExists) {
          Log.Warning($"Servant is missing but coffin exists for player {playerData.Name}; cleaning the orphaned coffin before replacement.");
          carrier.DestroyEntitiesSafely();
          Carriers.Remove(platformId);
          carrier = CreateCarrier(playerData);
          createdForThisSpawn = true;
        }
      }

      carrier.EnsureEntitiesEnabled();
      PendingSpawns.Add(platformId);
      ActionScheduler.DelayedFrames(
        () => TryCompleteSpawn(platformId, carrier, createdForThisSpawn, SpawnInitializationAttempts),
        SpawnInitializationDelayFrames
      );
    } catch (Exception ex) {
      PendingSpawns.Remove(platformId);
      RollbackFailedSpawn(platformId, carrier, createdForThisSpawn);
      Log.Error($"Failed to create carrier for player {playerData.Name} ({platformId}): {ex}");
    }
  }

  private static Carrier CreateCarrier(PlayerData playerData) {
    var carrier = new Carrier(playerData);
    Carriers[playerData.PlatformId] = carrier;
    try {
      carrier.Create();
      return carrier;
    } catch {
      carrier.DestroyEntitiesSafely();
      if (Carriers.TryGetValue(playerData.PlatformId, out var current) && ReferenceEquals(current, carrier)) {
        Carriers.Remove(playerData.PlatformId);
      }
      throw;
    }
  }

  private static void TryCompleteSpawn(ulong platformId, Carrier carrier, bool createdForThisSpawn, int attemptsRemaining) {
    if (!PendingSpawns.Contains(platformId)) return;
    if (!Carriers.TryGetValue(platformId, out var current) || !ReferenceEquals(current, carrier)) {
      PendingSpawns.Remove(platformId);
      return;
    }

    try {
      carrier.EnsureEntitiesEnabled();

      if (!carrier.IsValid() || !IsInventoryReady(carrier.ServantEntity)) {
        if (attemptsRemaining > 1) {
          ActionScheduler.DelayedFrames(
            () => TryCompleteSpawn(platformId, carrier, createdForThisSpawn, attemptsRemaining - 1),
            SpawnInitializationDelayFrames
          );
          return;
        }

        PendingSpawns.Remove(platformId);
        RollbackFailedSpawn(platformId, carrier, createdForThisSpawn);
        Log.Error($"Carrier entities or inventory did not become ready for player {platformId} after {SpawnInitializationAttempts} attempts.");
        return;
      }

      InventoryService.ModifyInventorySize(carrier.ServantEntity, 27);
      carrier.Call();
      PendingSpawns.Remove(platformId);
    } catch (Exception ex) {
      PendingSpawns.Remove(platformId);
      RollbackFailedSpawn(platformId, carrier, createdForThisSpawn);
      Log.Error($"Failed to initialize carrier for player {platformId}: {ex}");
    }
  }

  private static bool IsInventoryReady(Entity servant) {
    if (servant.IsNull() || !servant.Exists()) return false;
    if (!servant.Has<InventoryInstanceElement>()) return false;

    var inventoryInstances = servant.ReadBuffer<InventoryInstanceElement>();
    if (inventoryInstances.Length == 0) return false;

    if (!InventoryService.TryGetInventoryEntity(servant, out var inventoryEntity)) return false;
    if (inventoryEntity.IsNull() || !inventoryEntity.Exists()) return false;
    return inventoryEntity.Has<InventoryBuffer>();
  }

  private static void RollbackFailedSpawn(ulong platformId, Carrier carrier, bool createdForThisSpawn) {
    if (!createdForThisSpawn || carrier == null) return;

    carrier.DestroyEntitiesSafely();
    if (Carriers.TryGetValue(platformId, out var current) && ReferenceEquals(current, carrier)) {
      Carriers.Remove(platformId);
    }
  }

  public static void Dismiss(ulong platformId) {
    var playerData = platformId.GetPlayerData();
    PendingSpawns.Remove(platformId);

    if (!Carriers.TryGetValue(platformId, out var carrier)) {
      return;
    }

    var servantExists = carrier.ServantEntity.Exists();
    var coffinExists = carrier.CoffinEntity.Exists();

    if (!servantExists) {
      if (coffinExists) {
        Log.Warning($"Removing orphaned carrier coffin while dismissing player {platformId}.");
        carrier.DestroyEntitiesSafely();
      }
      Carriers.Remove(platformId);
      return;
    }

    try {
      carrier.Dismiss();
    } catch (Exception ex) {
      Log.Error($"Failed to dismiss carrier for player {playerData?.Name ?? platformId.ToString()} ({platformId}): {ex}");
      try {
        carrier.Hide();
      } catch (Exception fallbackEx) {
        Log.Error($"Failed to hide carrier after dismiss failure for player {platformId}: {fallbackEx}");
      }
    }
  }

  public static void RemoveCarrier(ulong platformId) {
    if (HasServant(platformId)) {
      Carriers.Remove(platformId);
    }
  }

  public static Carrier GetCarrier(ulong platformId) {
    if (Carriers.TryGetValue(platformId, out var carrier) && carrier.IsValid()) {
      return carrier;
    }
    return null;
  }

  public static bool HasServant(ulong platformId) {
    return Carriers.TryGetValue(platformId, out var carrier) && carrier.IsValid();
  }

  public static bool IsFollowing(ulong platformId) {
    return Carriers.TryGetValue(platformId, out var carrier) && carrier.IsFollowing;
  }

  public static void ToggleFollow(ulong platformId) {
    if (Carriers.TryGetValue(platformId, out var carrier)) {
      carrier.ToggleFollow();
    }
  }

  public static void StartFollow(ulong platformId) {
    if (Carriers.TryGetValue(platformId, out var carrier)) {
      carrier.StartFollow();
    }
  }

  public static void StopFollow(ulong platformId) {
    if (Carriers.TryGetValue(platformId, out var carrier)) {
      carrier.StopFollow();
    }
  }

  public static void ClearAllLegacy() {
    var query = GameSystems.EntityManager.CreateEntityQuery(new EntityQueryDesc {
      All = new[] { ComponentType.ReadOnly<ServantCoffinstation>() },
      Options = EntityQueryOptions.IncludeDisabled
    }).ToEntityArray(Allocator.Temp);

    foreach (var coffin in query) {
      ClearLegacyCoffinFromWorld(coffin);
    }
  }

  private static void ClearLegacyCoffinFromWorld(Entity coffin) {
    if (Entity.Null.Equals(coffin) || !coffin.Has<ServantCoffinstation>()) return;

    if (!coffin.Has<NameableInteractable>() || !coffin.Has<LocalTransform>()) return;

    var position = coffin.Read<LocalTransform>().Position;
    var id = coffin.Read<NameableInteractable>().Name.Value;

    if (position.y != Carrier.LegacyHeight || id != Carrier.LegacyId) return;
    var servant = coffin.Read<ServantCoffinstation>().ConnectedServant._Entity;

    if (!Entity.Null.Equals(servant) && servant.Has<Follower>()) {
      ClearLegacyServantFromWorld(servant);
    }

    var coffinBuffBuffer = coffin.ReadBuffer<BuffBuffer>();

    foreach (var buff in coffinBuffBuffer) {
      BuffService.TryRemoveBuff(coffin, buff.PrefabGuid);
    }

    Log.Info($"Clearing legacy coffin {coffin} from world.");

    coffin.Destroy();
  }

  private static void ClearLegacyServantFromWorld(Entity servant) {
    if (Entity.Null.Equals(servant) || !servant.Has<Follower>()) return;

    servant.Remove<Follower>();

    InventoryService.ClearInventory(servant);

    var servantBuffBuffer = servant.ReadBuffer<BuffBuffer>();

    foreach (var buff in servantBuffBuffer) {
      BuffService.TryRemoveBuff(servant, buff.PrefabGuid);
    }

    Log.Info($"Clearing legacy servant {servant} from world.");

    servant.Destroy();
  }
}
