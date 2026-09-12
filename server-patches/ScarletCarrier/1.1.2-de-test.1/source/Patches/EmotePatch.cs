using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using ScarletCore;
using Unity.Collections;
using ScarletCore.Utils;
using ScarletCarrier.Services;
using System.Collections.Generic;
using Stunlock.Core;
using System;
using ScarletCore.Systems;

namespace ScarletCarrier.Patches;

[HarmonyPatch(typeof(EmoteSystem), nameof(EmoteSystem.OnUpdate))]
public static class EmoteSystemPatch {
  private static readonly Dictionary<PrefabGUID, Action<ulong>> EmoteActions = new() {
    { new(-1525577000), CarrierService.Spawn },
    { new(-53273186), CarrierService.Dismiss },
    { new(-452406649), CarrierService.ToggleFollow }
  };

  [HarmonyPrefix]
  static void OnUpdatePrefix(EmoteSystem __instance) {
    if (!GameSystems.Initialized) return;
    var entities = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      foreach (var entity in entities) {
        if (!entity.Exists()) continue;

        var useEmoteEvent = entity.Read<UseEmoteEvent>();
        var fromCharacter = entity.Read<FromCharacter>();
        var emoteGuid = useEmoteEvent.Action;
        var user = fromCharacter.User;

        if (!user.Exists()) {
          continue;
        }

        var player = user.GetPlayerData();

        if (player == null) {
          continue;
        }

        var platformId = player.PlatformId;

        if (!EmoteActions.TryGetValue(emoteGuid, out var action)) {
          continue;
        }

        if (Plugin.Database.Get<List<ulong>>("DisabledEmotes")?.Contains(platformId) == true) {
          continue;
        }

        entity.Destroy(true);

        action(platformId);
      }
    } catch (Exception ex) {
      Log.Error($"Error in EmoteSystemPatch: {ex.Message}");
    } finally {
      entities.Dispose();
    }
  }
}