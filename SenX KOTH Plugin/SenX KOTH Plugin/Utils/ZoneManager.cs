using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SenX_KOTH_Plugin.Models;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.ObjectBuilders;
using VRageMath;

namespace SenX_KOTH_Plugin.Utils;

internal static class ZoneManager
{
    private static readonly Logger Log = LogManager.GetLogger("KoTH Plugin => ZoneManager");
    private static readonly Random _rng = new();

    public static readonly List<KothZoneCache> ZoneCache = [];
    public static bool IsInitialized { get; private set; }

    public static void Initialize(ZoneListData data)
    {
        ZoneCache.Clear();
        IsInitialized = false;
    }

    public static void CacheZone(string name, Vector3D position, float radius, long entityId, KothZone config)
    {
        var existing = ZoneCache.FirstOrDefault(z =>
            string.Equals(z.ZoneName, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            ZoneCache.Remove(existing);

        ZoneCache.Add(new KothZoneCache
        {
            ZoneName = name,
            Position = position,
            Radius = radius,
            RadiusSq = radius * radius,
            SafeZoneEntityId = entityId,
            Config = config
        });
    }

    public static MySafeZone? FindExistingSafeZone(string name)
    {
        if (MySession.Static == null) return null;

        var prefixName = "[KoTH] " + name;
        foreach (var sz in MySessionComponentSafeZones.SafeZones)
        {
            if (sz == null || sz.Closed || sz.MarkedForClose) continue;
            if (string.Equals(sz.DisplayName, prefixName, StringComparison.OrdinalIgnoreCase))
                return sz;
        }
        return null;
    }

    public static void CreateSafeZoneEntity(KothZone zone, Vector3D position)
    {
        try
        {
            var ob = new MyObjectBuilder_SafeZone
            {
                PositionAndOrientation = new MyPositionAndOrientation(MatrixD.CreateWorld(position)),
                Radius = zone.Radius,
                Enabled = zone.Enabled,
                Shape = MySafeZoneShape.Sphere,
                DisplayName = "[KoTH] " + zone.Name,
                AccessTypePlayers = MySafeZoneAccess.Blacklist,
                AccessTypeFactions = MySafeZoneAccess.Blacklist,
                AccessTypeGrids = MySafeZoneAccess.Blacklist,
                AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist,
                AllowedActions = MySafeZoneAction.Damage | MySafeZoneAction.Shooting,
                ModelColor = new SerializableVector3(
                    Math.Max(0f, Math.Min(1f, zone.ColorR)),
                    Math.Max(0f, Math.Min(1f, zone.ColorG)),
                    Math.Max(0f, Math.Min(1f, zone.ColorB))),
                Texture = zone.Texture,
                IsVisible = zone.Enabled || zone.PersistVisual,
                PersistentFlags = MyPersistentEntityFlags2.InScene
            };

            GameThread.Invoke(() =>
            {
                MyEntity? newEntity = MyEntities.CreateFromObjectBuilderAndAdd(ob, fadeIn: false);
                if (newEntity == null)
                {
                    Log.Error("Failed to create safe zone entity for: " + zone.Name);
                    return;
                }
                
                MyEntities.Add(newEntity);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create safe zone entity for: " + zone.Name);
        }
    }

    public static KothZone? CreateZone(ZoneListData data, string name, float radius, Vector3D position, string adminName)
    {
        if (data.Zones.Any(z => string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Warn(adminName + " tried to create duplicate zone: " + name);
            return null;
        }

        var zone = new KothZone
        {
            Name = name,
            X = position.X, Y = position.Y, Z = position.Z,
            OriginX = position.X, OriginY = position.Y, OriginZ = position.Z,
            Radius = Math.Max(10f, Math.Min(500f, radius)),
            ColorR = 0.529f, ColorG = 0.808f, ColorB = 0.922f,
            Texture = "SafeZone_Texture_Default",
            Enabled = true, PersistVisual = true
        };

        CreateSafeZoneEntity(zone, position);
        data.Zones.Add(zone);
        return zone;
    }

    public static bool DeleteZone(ZoneListData data, string name)
    {
        var config = data.Zones.FirstOrDefault(z =>
            string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase));
        if (config == null) return false;

        var safeZone = FindExistingSafeZone(name);
        if (safeZone != null)
        {
            try
            {
                GameThread.Invoke(() => safeZone.Close());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to close safe zone entity for: " + name);
            }
        }

        var cacheEntry = ZoneCache.FirstOrDefault(z =>
            string.Equals(z.ZoneName, name, StringComparison.OrdinalIgnoreCase));
        if (cacheEntry != null)
            ZoneCache.Remove(cacheEntry);

        data.Zones.Remove(config);
        return true;
    }

    public static void UpdateZoneEntity(KothZone zone)
    {
        try
        {
            var cacheEntry = ZoneCache.FirstOrDefault(z =>
                string.Equals(z.ZoneName, zone.Name, StringComparison.OrdinalIgnoreCase));
            if (cacheEntry != null && cacheEntry.IsEvictionActive) return;

            MySafeZone? safeZone = FindExistingSafeZone(zone.Name);
            if (safeZone == null) return;

            GameThread.Invoke(() =>
            {
                var ob = (MyObjectBuilder_SafeZone)safeZone.GetObjectBuilder();
                ob.EntityId = safeZone.EntityId;
                ob.PositionAndOrientation = new MyPositionAndOrientation(safeZone.PositionComp.WorldMatrixRef);
                ob.Radius = zone.Radius;
                ob.Shape = MySafeZoneShape.Sphere;
                ob.DisplayName = "[KoTH] " + zone.Name;
                ob.Enabled = zone.Enabled;
                ob.IsVisible = zone.Enabled || zone.PersistVisual;
                ob.AccessTypePlayers = MySafeZoneAccess.Blacklist;
                ob.AccessTypeFactions = MySafeZoneAccess.Blacklist;
                ob.AccessTypeGrids = MySafeZoneAccess.Blacklist;
                ob.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
                ob.AllowedActions = MySafeZoneAction.Damage | MySafeZoneAction.Shooting;
                ob.ModelColor = new SerializableVector3(
                    Math.Max(0f, Math.Min(1f, zone.ColorR)),
                    Math.Max(0f, Math.Min(1f, zone.ColorG)),
                    Math.Max(0f, Math.Min(1f, zone.ColorB)));
                ob.Texture = zone.Texture;

                ForceInitSafeZone(safeZone, ob, true);
            });

            if (cacheEntry != null)
            {
                cacheEntry.Radius = zone.Radius;
                cacheEntry.RadiusSq = zone.Radius * zone.Radius;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update safe zone entity for: " + zone.Name);
        }
    }

    public static void ApplyEvictionState(KothZoneCache cache, float? r, float? g, float? b, string? texture)
    {
        Log.Info("Applying  eviction state...");
        try
        {
            cache.IsEvictionActive = true;

            MySafeZone? safeZone = FindExistingSafeZone(cache.ZoneName);
            
            if (safeZone == null)
            {
                Log.Error($"Unable to find SafeZone for {cache.ZoneName} with entityid {cache.SafeZoneEntityId}.");
                return;
            }
            
            GameThread.Invoke(() =>
            {
                var ob = (MyObjectBuilder_SafeZone)safeZone.GetObjectBuilder();
                ob.EntityId = safeZone.EntityId;
                ob.PositionAndOrientation = new MyPositionAndOrientation(safeZone.PositionComp.WorldMatrixRef);
                ob.Radius = safeZone.Radius;
                ob.Size = safeZone.Size;

                ob.Texture = texture ?? "SafeZone_Texture_Default";
                ob.AllowedActions = 0;
                ob.AccessTypePlayers = MySafeZoneAccess.Whitelist;
                ob.AccessTypeFactions = MySafeZoneAccess.Whitelist;
                ob.AccessTypeGrids = MySafeZoneAccess.Whitelist;
                ob.AccessTypeFloatingObjects = MySafeZoneAccess.Whitelist;
                ob.Enabled = true;
                ob.IsVisible = true;

                if (r.HasValue && g.HasValue && b.HasValue)
                {
                    ob.ModelColor = new SerializableVector3(r.Value, g.Value, b.Value);
                }

                // Force internal update ignoring Keen's collision safety checks
                ForceInitSafeZone(safeZone, ob, true);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply eviction state: " + cache.ZoneName);
        }
    }
    
    private static readonly MethodInfo? InitInternalMethod = typeof(MySafeZone).GetMethod("InitInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [typeof(MyObjectBuilder_SafeZone), typeof(bool)], null);
    private static void ForceInitSafeZone(MySafeZone safeZone, MyObjectBuilder_SafeZone ob, bool sync = true)
    {
        // Call InitInternal(ob, sync) directly
        InitInternalMethod?.Invoke(safeZone, [ob, sync]);
    
        // Broadcast network update to connected clients
        MySessionComponentSafeZones.RequestUpdateSafeZone(ob);
    }

    public static void RestoreEvictionState(KothZoneCache cache)
    {
        try
        {
            MySafeZone? safeZone = FindExistingSafeZone(cache.ZoneName);
            
            if (safeZone == null)
            {
                Log.Error($"Unable to find SafeZone for {cache.ZoneName} with entityid {cache.SafeZoneEntityId}.");
                return;
            }

            GameThread.Invoke(() =>
            {
                var ob = (MyObjectBuilder_SafeZone)safeZone.GetObjectBuilder();
                ob.Texture = cache.Config?.Texture ?? "SafeZone_Texture_Default";
                ob.AccessTypePlayers = MySafeZoneAccess.Blacklist;
                ob.AccessTypeFactions = MySafeZoneAccess.Blacklist;
                ob.AccessTypeGrids = MySafeZoneAccess.Blacklist;
                ob.AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist;
                ob.AllowedActions = MySafeZoneAction.Damage | MySafeZoneAction.Shooting;

                if (cache.Config != null)
                {
                    ob.ModelColor = new SerializableVector3(cache.Config.ColorR, cache.Config.ColorG, cache.Config.ColorB);
                }

                // Force internal update ignoring Keen's collision safety checks
                ForceInitSafeZone(safeZone, ob, true);
            });

            cache.IsEvictionActive = false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to restore zone state: " + cache.ZoneName);
        }
    }

    public static void MoveDynamicZone(KothZoneCache cache, KothZone zone)
    {
        try
        {
            MySafeZone? old = FindExistingSafeZone(zone.Name);
            if (old == null) return;

            var origin = zone.OriginVector3D;
            Vector3D newPos;
            int attempts = 0;
            do
            {
                newPos = RandomPositionInAnnulus(origin, zone.MinDistance, zone.MaxDistance);
                attempts++;
            }
            while (attempts < 20 && MySessionComponentSafeZones.IsSafeZoneColliding(
                old.EntityId, MatrixD.CreateWorld(newPos), MySafeZoneShape.Sphere, zone.Radius));

            GameThread.Invoke(() =>
            {
                old.Close();

                var ob = new MyObjectBuilder_SafeZone
                {
                    PositionAndOrientation = new MyPositionAndOrientation(MatrixD.CreateWorld(newPos)),
                    Radius = zone.Radius,
                    Enabled = zone.Enabled,
                    Shape = MySafeZoneShape.Sphere,
                    DisplayName = "[KoTH] " + zone.Name,
                    AccessTypePlayers = MySafeZoneAccess.Blacklist,
                    AccessTypeFactions = MySafeZoneAccess.Blacklist,
                    AccessTypeGrids = MySafeZoneAccess.Blacklist,
                    AccessTypeFloatingObjects = MySafeZoneAccess.Blacklist,
                    AllowedActions = MySafeZoneAction.Damage | MySafeZoneAction.Shooting,
                    ModelColor = new SerializableVector3(
                        Math.Max(0f, Math.Min(1f, zone.ColorR)),
                        Math.Max(0f, Math.Min(1f, zone.ColorG)),
                        Math.Max(0f, Math.Min(1f, zone.ColorB))),
                    Texture = zone.Texture,
                    IsVisible = zone.Enabled || zone.PersistVisual,
                    PersistentFlags = MyPersistentEntityFlags2.InScene
                };

                var newEntity = MyEntities.CreateFromObjectBuilderAndAdd(ob, false);
                MyEntities.Add(newEntity);
                cache.SafeZoneEntityId = newEntity.EntityId;
                cache.Position = newPos;

                if (zone.AnnounceGps)
                {
                    MyVisualScriptLogicProvider.ShowNotificationToAll(
                        "[KoTH] " + zone.Name + " moved — GPS: " + newPos.ToString("F0"), 10000, "White");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to move dynamic zone: " + cache.ZoneName);
        }
    }

    public static Vector3D RandomPositionInAnnulus(Vector3D origin, double minDist, double maxDist)
    {
        double distance = minDist + _rng.NextDouble() * (maxDist - minDist);
        double theta = _rng.NextDouble() * Math.PI * 2;
        double phi = Math.Acos(2 * _rng.NextDouble() - 1);
        return new Vector3D(
            origin.X + distance * Math.Sin(phi) * Math.Cos(theta),
            origin.Y + distance * Math.Sin(phi) * Math.Sin(theta),
            origin.Z + distance * Math.Cos(phi));
    }
}

internal sealed class KothZoneCache
{
    public string ZoneName { get; set; } = "";
    public Vector3D Position { get; set; }
    public float Radius { get; set; }
    public double RadiusSq { get; set; }
    public long SafeZoneEntityId { get; set; }
    public bool IsEvictionActive { get; set; }
    public KothZone? Config { get; set; }
}