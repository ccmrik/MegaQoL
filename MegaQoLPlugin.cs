using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MegaQoL
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MegaQoLPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.rik.megaqol";
        public const string PluginName = "Mega QoL";
        public const string PluginVersion = "1.13.0";

        internal static ManualLogSource _logger;
        private static Harmony _harmony;
        private static ConfigFile _config;
        private static FileSystemWatcher _configWatcher;

        // Auto Repair
        public static ConfigEntry<bool> EnableAutoRepair;
        public static ConfigEntry<float> AutoRepairRange;
        public static ConfigEntry<float> AutoRepairInterval;

        // Refueller
        public static ConfigEntry<bool> EnableAutoRefuel;
        public static ConfigEntry<float> AutoRefuelRange;
        public static ConfigEntry<float> AutoRefuelInterval;

        // Ballista
        public static ConfigEntry<bool> EnableBallistaAutoReload;
        public static ConfigEntry<float> BallistaAutoReloadRange;
        public static ConfigEntry<float> BallistaAutoReloadInterval;
        public static ConfigEntry<bool> BallistaAutoReloadUseChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseReinforcedChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseBlackMetalChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseBarrels;
        public static ConfigEntry<bool> EnableBallistaImprovements;
        public static ConfigEntry<float> BallistaVelocityMultiplier;
        public static ConfigEntry<float> BallistaFireRate;
        public static ConfigEntry<float> BallistaAimAccuracy;
        public static ConfigEntry<float> BallistaTurnRate;
        public static ConfigEntry<float> BallistaRange;

        // Map Teleport
        public static ConfigEntry<bool> EnableMapTeleport;

        // No Mist
        public static ConfigEntry<bool> EnableNoMist;

        // MessageHud Smart Queue
        public static ConfigEntry<bool> EnableMessageHudQueue;

        // Instant Mining
        public static ConfigEntry<bool> EnableAOEMining;
        public static ConfigEntry<KeyCode> AOEMiningKey;

        // Debug
        public static ConfigEntry<bool> DebugMode;

        // Timers
        private static float _autoRefuelTimer = 0f;
        private static float _ballistaAutoReloadTimer = 0f;
        private static float _autoRepairTimer = 0f;

        private void Awake()
        {
            _logger = Logger;

            MigrateConfig(Config.ConfigFilePath);
            Config.Reload();

            // 1. Auto Repair
            EnableAutoRepair = Config.Bind("1. Auto Repair", "Enable", true,
                "Automatically repair all nearby build pieces when enabled");
            AutoRepairRange = Config.Bind("1. Auto Repair", "Range", 30f,
                new ConfigDescription("Range for auto-repair in meters", new AcceptableValueRange<float>(1f, 1000f)));
            AutoRepairInterval = Config.Bind("1. Auto Repair", "Interval", 3f,
                new ConfigDescription("How often to repair nearby pieces (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));

            // 2. Refueller
            EnableAutoRefuel = Config.Bind("2. Refueller", "Enable", true,
                "Automatically refuel nearby fire sources (campfires, torches, braziers, etc)");
            AutoRefuelRange = Config.Bind("2. Refueller", "Range", 10f,
                new ConfigDescription("Range for auto-refueling fires in meters", new AcceptableValueRange<float>(0f, 1000f)));
            AutoRefuelInterval = Config.Bind("2. Refueller", "Interval", 2f,
                new ConfigDescription("How often to check for fires to refuel (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));

            // 3. Ballista
            EnableBallistaAutoReload = Config.Bind("3. Ballista", "EnableAutoReload", true,
                "Automatically reload ballistas from nearby containers");
            BallistaAutoReloadRange = Config.Bind("3. Ballista", "AutoReloadRange", 10f,
                new ConfigDescription("Range to search for containers in meters", new AcceptableValueRange<float>(0f, 1000f)));
            BallistaAutoReloadInterval = Config.Bind("3. Ballista", "AutoReloadInterval", 5f,
                new ConfigDescription("How often to reload ballistas (seconds)", new AcceptableValueRange<float>(1f, 60f)));
            BallistaAutoReloadUseChests = Config.Bind("3. Ballista", "AutoReloadUseChests", true,
                "Allow reloading from regular Chests");
            BallistaAutoReloadUseReinforcedChests = Config.Bind("3. Ballista", "AutoReloadUseReinforcedChests", true,
                "Allow reloading from Reinforced Chests");
            BallistaAutoReloadUseBlackMetalChests = Config.Bind("3. Ballista", "AutoReloadUseBlackMetalChests", true,
                "Allow reloading from Black Metal Chests");
            BallistaAutoReloadUseBarrels = Config.Bind("3. Ballista", "AutoReloadUseBarrels", true,
                "Allow reloading from Barrels");
            EnableBallistaImprovements = Config.Bind("3. Ballista", "EnableFriendlyFirePrevention", true,
                "Prevents ballistas from shooting players or tamed creatures");
            BallistaVelocityMultiplier = Config.Bind("3. Ballista", "VelocityMultiplier", 1f,
                new ConfigDescription("Projectile speed multiplier (1 = vanilla, higher = faster bolts, auto-adjusts prediction)", new AcceptableValueRange<float>(1f, 10f)));
            BallistaFireRate = Config.Bind("3. Ballista", "FireRate", 1f,
                new ConfigDescription("Fire rate multiplier (1 = vanilla 1 shot/2sec, 10 = 10x faster)", new AcceptableValueRange<float>(1f, 10f)));
            BallistaAimAccuracy = Config.Bind("3. Ballista", "AimAccuracy", 1f,
                new ConfigDescription("Accuracy multiplier — tightens aim threshold AND reduces bolt spread (1 = vanilla, 10 = near-perfect accuracy)", new AcceptableValueRange<float>(1f, 10f)));
            BallistaTurnRate = Config.Bind("3. Ballista", "TurnRate", 45f,
                new ConfigDescription("Turret rotation speed deg/sec (higher = faster tracking, vanilla = 45)", new AcceptableValueRange<float>(45f, 500f)));
            BallistaRange = Config.Bind("3. Ballista", "Range", 30f,
                new ConfigDescription("Targeting range in meters (vanilla = 30)", new AcceptableValueRange<float>(30f, 200f)));

            // 6. Instant Mining
            EnableAOEMining = Config.Bind("5. Instant Mining", "Enable", true,
                "Hold hotkey while mining to instantly destroy the entire rock/ore deposit in one hit");
            AOEMiningKey = Config.Bind("5. Instant Mining", "Hotkey", KeyCode.LeftShift,
                "Hold this key while pickaxing to instant-mine the target");

            // 8. Map Teleport (sections 6 and 7 — Build Dust Removal & Rune Build —
            // moved to MegaBuilder v1.5.0+; their orphan cfg blocks are auto-pruned).
            EnableMapTeleport = Config.Bind("8. Map Teleport", "Enable", true,
                "Enables map teleportation - middle-click on map to teleport to that location");

            // 9. No Mist
            EnableNoMist = Config.Bind("9. No Mist", "Enable", true,
                "Removes all mist particle effects (Mistlands fog, swamp mist, etc)");

            // 10. MessageHud Smart Queue
            EnableMessageHudQueue = Config.Bind("10. MessageHud Smart Queue", "Enable", true,
                "Enables smart message queue - clears stale messages so the latest one shows immediately");

            // 99. Debug — standardised section across all Mega mods (v1.11.0+)
            DebugMode = Config.Bind("99. Debug", "DebugMode", false,
                "Enable verbose debug logging to BepInEx console/log");

            _config = Config;

            // Sweep orphan sections/keys left behind by previous versions (Item Management,
            // Pet Feeder, Craft from Containers, Mass Farming, Build Dust Removal, etc.
            // — all extracted into MegaStuff/MegaFarming over v1.7+ but left behind in user
            // cfgs). Runs AFTER all Bind() calls so the snapshot is complete, BEFORE the
            // watcher attaches so we don't trigger a phantom Reload.
            ConfigPruner.Prune(_config, _logger);

            SetupConfigWatcher();

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            _logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");
            Log($"Live config reloading enabled - edit {Config.ConfigFilePath} and save to apply changes!");
        }

        private static void MigrateConfig(string configPath)
        {
            try
            {
                if (!File.Exists(configPath)) return;
                string text = File.ReadAllText(configPath);
                bool changed = false;

                // Delete truly obsolete sections
                changed |= MigrateCfgSection(ref text, "3. Ballista Reloader", null);
                changed |= MigrateCfgSection(ref text, "8. Ballista Improvements", null);
                changed |= MigrateCfgSection(ref text, "10b. Rune Build", null);

                // Migrate renumbered sections from v1.8.4 → v1.8.5
                changed |= MigrateCfgSection(ref text, "7. Map Teleport", "12. Map Teleport");
                changed |= MigrateCfgSection(ref text, "9. Build Dust Removal", "10. Build Dust Removal");
                changed |= MigrateCfgSection(ref text, "10. Rune Build", "11. Rune Build");
                changed |= MigrateCfgSection(ref text, "11. No Mist", "13. No Mist");
                changed |= MigrateCfgSection(ref text, "12. MessageHud Smart Queue", "14. MessageHud Smart Queue");
                changed |= MigrateCfgSection(ref text, "13. Mass Farming", "7. Mass Farming");
                changed |= MigrateCfgSection(ref text, "14. Instant Mining", "9. Instant Mining");

                // v1.8.7 → v1.8.8: clean up stale Debug renumber
                changed |= MigrateCfgSection(ref text, "14. Debug", "16. Debug");

                // v1.9.20: Summoned Skeletons moved to MegaSkeletons mod, Debug → 15
                changed |= MigrateCfgSection(ref text, "16. Debug", "15. Debug");
                changed |= MigrateCfgSection(ref text, "15. Summoned Skeletons", null);

                // v1.11.0: standardise debug section to "99. Debug" across every Mega mod
                changed |= MigrateCfgSection(ref text, "11. Debug", "99. Debug");
                changed |= MigrateCfgSection(ref text, "12. Debug", "99. Debug");
                changed |= MigrateCfgSection(ref text, "15. Debug", "99. Debug");

                // v1.9.6: remove obsolete FiringVelocity key (replaced by VelocityMultiplier)
                changed |= MigrateCfgKey(ref text, "FiringVelocity");
                // v1.9.9: remove Prediction key (now auto-computed from velocity)
                changed |= MigrateCfgKey(ref text, "Prediction");

                if (changed)
                    File.WriteAllText(configPath, text.TrimEnd() + "\n");
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }
        }

        private static bool MigrateCfgSection(ref string text, string oldName, string newName)
        {
            string oldHeader = "[" + oldName + "]";
            int idx = text.IndexOf(oldHeader, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            int sectionEnd = text.IndexOf("\n[", idx + oldHeader.Length, StringComparison.Ordinal);

            if (newName == null || text.IndexOf("[" + newName + "]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (sectionEnd < 0)
                    text = text.Substring(0, idx).TrimEnd('\r', '\n');
                else
                    text = text.Substring(0, idx) + text.Substring(sectionEnd + 1);
            }
            else
            {
                text = text.Remove(idx, oldHeader.Length).Insert(idx, "[" + newName + "]");
            }
            return true;
        }

        private static bool MigrateCfgKey(ref string text, string keyName)
        {
            // Remove a single "Key = Value" line (plus any preceding comment lines)
            int idx = text.IndexOf(keyName + " = ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            // Find start of line
            int lineStart = text.LastIndexOf('\n', idx);
            lineStart = (lineStart < 0) ? 0 : lineStart;
            // Find end of line
            int lineEnd = text.IndexOf('\n', idx);
            if (lineEnd < 0) lineEnd = text.Length;
            // Also remove preceding comment line(s) starting with ##
            while (lineStart > 0)
            {
                int prevLineStart = text.LastIndexOf('\n', lineStart - 1);
                if (prevLineStart < 0) prevLineStart = 0;
                string prevLine = text.Substring(prevLineStart, lineStart - prevLineStart).Trim();
                if (prevLine.StartsWith("##") || prevLine.StartsWith("//") || prevLine == "")
                    lineStart = prevLineStart;
                else
                    break;
            }
            text = text.Substring(0, lineStart) + text.Substring(lineEnd);
            return true;
        }

        private void SetupConfigWatcher()
        {
            string configPath = Path.GetDirectoryName(Config.ConfigFilePath);
            string configFile = Path.GetFileName(Config.ConfigFilePath);

            _configWatcher = new FileSystemWatcher(configPath, configFile);
            _configWatcher.Changed += OnConfigChanged;
            _configWatcher.Created += OnConfigChanged;
            _configWatcher.Renamed += OnConfigChanged;
            _configWatcher.IncludeSubdirectories = false;
            _configWatcher.SynchronizingObject = null;
            _configWatcher.EnableRaisingEvents = true;

            Log($"Config watcher started for: {configFile}");
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(100);
                _config.Reload();
                Log("Config reloaded! Changes applied.");
                float _velMult = BallistaVelocityMultiplier.Value;
                float _aimAcc = BallistaAimAccuracy.Value;
                float _vanPred = Turret_Awake_Patch.VanillaPredictionModifier / _velMult;
                float _perfPred = 1f / _velMult;
                float _predT = (_aimAcc - 1f) / 9f;
                float _finalPred = Mathf.Lerp(_vanPred, _perfPred, _predT);
                if (DebugMode.Value)
                    _logger.LogInfo($"[Ballista] Config values: FireRate={BallistaFireRate.Value}x, AimAccuracy={_aimAcc}x, TurnRate={BallistaTurnRate.Value}, Range={BallistaRange.Value}, VelMultiplier={_velMult}x, Prediction={_finalPred:F3} (spread={(1f/(_aimAcc*_aimAcc)):F3})");

                if (Player.m_localPlayer != null)
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "MegaQoL Config Reloaded!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to reload config: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (_configWatcher != null)
            {
                _configWatcher.EnableRaisingEvents = false;
                _configWatcher.Dispose();
                _configWatcher = null;
            }
            _harmony?.UnpatchSelf();
        }

        public static void Log(string message) { if (DebugMode.Value) _logger?.LogInfo(message); }
        public static void LogWarning(string message) => _logger?.LogWarning(message);
        public static void LogError(string message) => _logger?.LogError(message);

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            // Auto-repair nearby build pieces
            if (EnableAutoRepair.Value)
            {
                _autoRepairTimer += Time.deltaTime;
                if (_autoRepairTimer >= AutoRepairInterval.Value)
                {
                    _autoRepairTimer = 0f;
                    AutoRepairHelper.RepairNearbyPieces(player.transform.position, AutoRepairRange.Value);
                }
            }

            // Auto-refuel nearby fire sources
            if (EnableAutoRefuel.Value)
            {
                _autoRefuelTimer += Time.deltaTime;
                if (_autoRefuelTimer >= AutoRefuelInterval.Value)
                {
                    _autoRefuelTimer = 0f;
                    AutoRefuelHelper.RefuelNearbyFireSources(player.transform.position, AutoRefuelRange.Value);
                }
            }

            // Ballista auto reload
            if (EnableBallistaAutoReload.Value)
            {
                _ballistaAutoReloadTimer += Time.deltaTime;
                if (_ballistaAutoReloadTimer >= BallistaAutoReloadInterval.Value)
                {
                    _ballistaAutoReloadTimer = 0f;
                    BallistaAutoReloadHelper.ReloadNearbyBallistas(player.transform.position, BallistaAutoReloadRange.Value);
                }
            }

            // Speed and jump now handled by MegaTrainer
        }
    }

    // ==================== AUTO REPAIR ======================================

    public static class AutoRepairHelper
    {
        public static void RepairNearbyPieces(Vector3 position, float range)
        {
            var allInstances = WearNTear.GetAllInstances();
            if (allInstances == null) return;

            float rangeSq = range * range;
            foreach (var wnt in allInstances)
            {
                if (wnt == null) continue;
                if ((position - wnt.transform.position).sqrMagnitude > rangeSq) continue;
                // Only repair player-placed build pieces, not world objects
                var piece = wnt.GetComponent<Piece>();
                if (piece == null || piece.GetCreator() == 0) continue;
                if (wnt.GetHealthPercentage() >= 1f) continue;
                wnt.Repair();
            }
        }
    }

    // ==================== AUTO-REFUEL ====================

    public static class AutoRefuelHelper
    {
        // Cached registries — updated by Harmony patches
        public static readonly HashSet<Fireplace> AllFireplaces = new HashSet<Fireplace>();
        public static readonly HashSet<CookingStation> AllCookingStations = new HashSet<CookingStation>();

        public static void RefuelNearbyFireSources(Vector3 position, float range)
        {
            float rangeSq = range * range;

            foreach (var fireplace in AllFireplaces)
            {
                if (fireplace == null) continue;
                if ((position - fireplace.transform.position).sqrMagnitude > rangeSq) continue;
                RefuelFireplace(fireplace);
            }

            foreach (var station in AllCookingStations)
            {
                if (station == null) continue;
                if ((position - station.transform.position).sqrMagnitude > rangeSq) continue;

                string objectName = station.gameObject.name.ToLower();
                if (objectName.Contains("oven"))
                    RefuelCookingStation(station);
            }
        }

        private static void RefuelFireplace(Fireplace fireplace)
        {
            if (fireplace == null) return;
            var nview = fireplace.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            try
            {
                float maxFuel = fireplace.m_maxFuel;
                float currentFuel = nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
                if (currentFuel < maxFuel - 0.01f)
                    nview.GetZDO().Set(ZDOVars.s_fuel, maxFuel);
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }
        }

        private static void RefuelCookingStation(CookingStation station)
        {
            if (station == null) return;
            var nview = station.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            try
            {
                float maxFuel = station.m_maxFuel;
                float currentFuel = nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
                if (currentFuel < maxFuel - 0.01f)
                    nview.GetZDO().Set(ZDOVars.s_fuel, maxFuel);
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }
        }
    }

    // ==================== BALLISTA AUTO RELOAD ====================

    public static class BallistaAutoReloadHelper
    {
        public static readonly HashSet<Turret> AllTurrets = new HashSet<Turret>();

        public static void ReloadNearbyBallistas(Vector3 position, float range)
        {
            float rangeSq = range * range;

            foreach (var turret in AllTurrets)
            {
                if (turret == null) continue;
                if ((position - turret.transform.position).sqrMagnitude > rangeSq) continue;

                var nview = turret.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid()) continue;

                int currentAmmo = nview.GetZDO().GetInt(ZDOVars.s_ammo, 0);
                int maxAmmo = turret.m_maxAmmo;
                if (currentAmmo >= maxAmmo) continue;

                TryReloadTurret(turret, nview, position, range, currentAmmo, maxAmmo);
            }
        }

        private static void TryReloadTurret(Turret turret, ZNetView nview, Vector3 position, float range, int currentAmmo, int maxAmmo)
        {
            int ammoNeeded = maxAmmo - currentAmmo;

            List<ItemDrop> allowedAmmo = null;
            if (BallistaFriendlyHelper.AllowedAmmoField != null)
                allowedAmmo = BallistaFriendlyHelper.AllowedAmmoField.GetValue(turret) as List<ItemDrop>;

            var validAmmoNames = new List<string>();
            if (allowedAmmo != null && allowedAmmo.Count > 0)
            {
                foreach (var ammo in allowedAmmo)
                    if (ammo != null) validAmmoNames.Add(ammo.gameObject.name);
            }
            else
            {
                validAmmoNames.AddRange(new[] { "TurretBolt", "TurretBoltWood", "TurretBoltFlametal", "TurretBoltBone" });
            }

            foreach (var container in ContainerHelper.AllContainers)
            {
                if (container == null) continue;
                float distance = Vector3.Distance(position, container.transform.position);
                if (distance > range) continue;
                if (!IsAllowedContainer(container)) continue;

                var inventory = container.GetInventory();
                if (inventory == null) continue;

                foreach (var item in inventory.GetAllItems())
                {
                    if (item == null || item.m_stack <= 0) continue;

                    string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
                    if (string.IsNullOrEmpty(prefabName))
                        prefabName = item.m_shared.m_name.Replace("$item_", "");

                    bool isValidAmmo = false;
                    foreach (var validName in validAmmoNames)
                    {
                        if (prefabName.Equals(validName, StringComparison.OrdinalIgnoreCase) ||
                            item.m_shared.m_name.ToLower().Contains("bolt") ||
                            item.m_shared.m_name.ToLower().Contains("missile"))
                        {
                            isValidAmmo = true;
                            break;
                        }
                    }

                    if (isValidAmmo)
                    {
                        int ammoToTake = Mathf.Min(ammoNeeded, item.m_stack);
                        inventory.RemoveItem(item, ammoToTake);
                        int newAmmo = currentAmmo + ammoToTake;
                        nview.GetZDO().Set(ZDOVars.s_ammo, newAmmo);
                        if (item.m_dropPrefab != null)
                            nview.GetZDO().Set(ZDOVars.s_ammoType, item.m_dropPrefab.name);
                        ammoNeeded -= ammoToTake;
                        currentAmmo = newAmmo;
                        if (ammoNeeded <= 0) return;
                    }
                }
            }
        }

        private static bool IsAllowedContainer(Container container)
        {
            var type = ContainerHelper.GetContainerType(container);
            switch (type)
            {
                case ContainerType.BlackMetalChest: return MegaQoLPlugin.BallistaAutoReloadUseBlackMetalChests.Value;
                case ContainerType.Barrel: return MegaQoLPlugin.BallistaAutoReloadUseBarrels.Value;
                case ContainerType.ReinforcedChest: return MegaQoLPlugin.BallistaAutoReloadUseReinforcedChests.Value;
                case ContainerType.WoodChest: return MegaQoLPlugin.BallistaAutoReloadUseChests.Value;
                default: return false;
            }
        }
    }

    // ==================== CONTAINER TYPE ====================

    public enum ContainerType
    {
        Unknown,
        WoodChest,
        ReinforcedChest,
        BlackMetalChest,
        Barrel,
        Obliterator,
        Private
    }

    // ==================== CONTAINER HELPER (SLIM — for Refuel/Ballista) ====================

    public static class ContainerHelper
    {
        public static readonly HashSet<Container> AllContainers = new HashSet<Container>();
        private static readonly Dictionary<Container, ContainerType> _typeCache = new Dictionary<Container, ContainerType>();
        private static readonly List<Container> _nearbyCache = new List<Container>();
        private static Vector3 _nearbyCachePos;
        private static float _nearbyCacheRadius;
        private static float _nearbyCacheTime;
        private const float NEARBY_CACHE_TTL = 1.0f;
        private static float _lastPruneTime;
        private const float PRUNE_INTERVAL = 30f;

        public static ContainerType ClassifyContainer(Container container)
        {
            string name = container.gameObject.name.ToLower();
            if (name.Contains("private")) return ContainerType.Private;
            if (name.Contains("piece_chest_blackmetal") || name.StartsWith("blackmetalchest")) return ContainerType.BlackMetalChest;
            if (name.Contains("incinerator") || name.Contains("obliterator")) return ContainerType.Obliterator;
            if (name.Contains("barrel")) return ContainerType.Barrel;
            if ((name.Contains("piece_chest") || name.StartsWith("reinforcedchest")) && !name.Contains("wood") && !name.Contains("blackmetal")) return ContainerType.ReinforcedChest;
            if (name.Contains("chest")) return ContainerType.WoodChest;
            return ContainerType.Unknown;
        }

        public static void Register(Container container) { AllContainers.Add(container); _typeCache[container] = ClassifyContainer(container); }
        public static void Unregister(Container container) { AllContainers.Remove(container); _typeCache.Remove(container); _nearbyCacheTime = 0f; }

        public static ContainerType GetContainerType(Container container)
        {
            if (_typeCache.TryGetValue(container, out var type)) return type;
            return ContainerType.Unknown;
        }

        public static List<Container> FindNearbyContainers(Vector3 position, float radius)
        {
            float now = Time.time;
            if (now - _lastPruneTime > PRUNE_INTERVAL)
            {
                _lastPruneTime = now;
                AllContainers.RemoveWhere(c => c == null);
                var staleKeys = new List<Container>();
                foreach (var kvp in _typeCache) if (kvp.Key == null) staleKeys.Add(kvp.Key);
                foreach (var k in staleKeys) _typeCache.Remove(k);
            }
            if (now - _nearbyCacheTime < NEARBY_CACHE_TTL && Mathf.Approximately(_nearbyCacheRadius, radius) && (position - _nearbyCachePos).sqrMagnitude < 4f)
                return _nearbyCache;

            _nearbyCache.Clear();
            _nearbyCachePos = position;
            _nearbyCacheRadius = radius;
            _nearbyCacheTime = now;
            float radiusSq = radius * radius;
            foreach (var container in AllContainers)
            {
                if (container == null) continue;
                if ((position - container.transform.position).sqrMagnitude > radiusSq) continue;
                var type = GetContainerType(container);
                if (type == ContainerType.Private || type == ContainerType.Unknown) continue;
                _nearbyCache.Add(container);
            }
            return _nearbyCache;
        }
    }

    // ==================== CONTAINER REGISTRY PATCHES ====================

    [HarmonyPatch(typeof(Container), "Awake")]
    public static class Container_Awake_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Container __instance)
        {
            ContainerHelper.Register(__instance);
            if (__instance.gameObject.GetComponent<ContainerDestroyTracker>() == null)
                __instance.gameObject.AddComponent<ContainerDestroyTracker>();
        }
    }

    [HarmonyPatch(typeof(Container), "OnDestroyed")]
    public static class Container_OnDestroyed_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Container __instance)
        {
            ContainerHelper.Unregister(__instance);
        }
    }

    public class ContainerDestroyTracker : MonoBehaviour
    {
        private void OnDestroy()
        {
            var c = GetComponent<Container>();
            if (c != null) ContainerHelper.Unregister(c);
        }
    }

    // ==================== FIREPLACE / COOKING STATION REGISTRY PATCHES ====================

    [HarmonyPatch(typeof(Fireplace), "Awake")]
    public static class Fireplace_Awake_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance) => AutoRefuelHelper.AllFireplaces.Add(__instance);
    }

    // Fireplace has no OnDestroyed, so we patch the Unity OnDestroy via a MonoBehaviour helper
    [HarmonyPatch(typeof(Fireplace), "Awake")]
    public static class Fireplace_TrackDestroy_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance)
        {
            if (__instance.gameObject.GetComponent<FireplaceDestroyTracker>() == null)
                __instance.gameObject.AddComponent<FireplaceDestroyTracker>();
        }
    }

    public class FireplaceDestroyTracker : MonoBehaviour
    {
        private void OnDestroy()
        {
            var fp = GetComponent<Fireplace>();
            if (fp != null) AutoRefuelHelper.AllFireplaces.Remove(fp);
        }
    }

    [HarmonyPatch(typeof(CookingStation), "Awake")]
    public static class CookingStation_Awake_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CookingStation __instance)
        {
            AutoRefuelHelper.AllCookingStations.Add(__instance);
            if (__instance.gameObject.GetComponent<CookingStationDestroyTracker>() == null)
                __instance.gameObject.AddComponent<CookingStationDestroyTracker>();
        }
    }

    [HarmonyPatch(typeof(CookingStation), "OnDestroyed")]
    public static class CookingStation_OnDestroyed_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(CookingStation __instance) => AutoRefuelHelper.AllCookingStations.Remove(__instance);
    }

    public class CookingStationDestroyTracker : MonoBehaviour
    {
        private void OnDestroy()
        {
            var cs = GetComponent<CookingStation>();
            if (cs != null) AutoRefuelHelper.AllCookingStations.Remove(cs);
        }
    }

    // ==================== TURRET REGISTRY PATCHES ====================

    [HarmonyPatch(typeof(Turret), "Awake")]
    public static class Turret_Awake_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            BallistaAutoReloadHelper.AllTurrets.Add(__instance);
            if (__instance.gameObject.GetComponent<TurretDestroyTracker>() == null)
                __instance.gameObject.AddComponent<TurretDestroyTracker>();
        }
    }

    [HarmonyPatch(typeof(Turret), "OnDestroyed")]
    public static class Turret_OnDestroyed_Registry_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Turret __instance) => BallistaAutoReloadHelper.AllTurrets.Remove(__instance);
    }

    public class TurretDestroyTracker : MonoBehaviour
    {
        private void OnDestroy()
        {
            var t = GetComponent<Turret>();
            if (t != null) BallistaAutoReloadHelper.AllTurrets.Remove(t);
        }
    }

    // ==================== MAP TELEPORT ====================

    [HarmonyPatch(typeof(Minimap), "OnMapMiddleClick")]
    public static class Minimap_OnMapMiddleClick_Teleport_Patch
    {
        private static MethodInfo _screenToWorldMethod;

        [HarmonyPostfix]
        public static void Postfix(Minimap __instance)
        {
            if (!MegaQoLPlugin.EnableMapTeleport.Value) return;

            Player player = Player.m_localPlayer;
            if (player == null) return;

            try
            {
                if (_screenToWorldMethod == null)
                    _screenToWorldMethod = typeof(Minimap).GetMethod("ScreenToWorldPoint",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (_screenToWorldMethod == null) return;

                Vector3 mousePos = Input.mousePosition;
                Vector3 worldPos = (Vector3)_screenToWorldMethod.Invoke(__instance, new object[] { mousePos });

                float groundHeight = ZoneSystem.instance.GetGroundHeight(worldPos);
                if (groundHeight > worldPos.y)
                    worldPos.y = groundHeight + 1f;
                else
                    worldPos.y += 1f;

                __instance.SetMapMode(Minimap.MapMode.None);
                player.TeleportTo(worldPos, player.transform.rotation, true);
                player.Message(MessageHud.MessageType.Center, "Teleported!");
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }
        }
    }

    // ==================== BALLISTA IMPROVEMENTS ====================

    [HarmonyPatch(typeof(Turret), "Awake")]
    public static class Turret_Awake_Patch
    {
        // Cache vanilla prefab values from the first turret that spawns
        private static bool _vanillaCached = false;
        internal static float VanillaAttackCooldown;
        internal static float VanillaShootWhenAimDiff;
        internal static float VanillaPredictionModifier;
        internal static float VanillaTurnRate;
        internal static float VanillaViewDistance;
        internal static float VanillaLookAcceleration;
        internal static float VanillaLookDeacceleration;

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            // Capture vanilla values ONCE before overriding anything
            if (!_vanillaCached)
            {
                VanillaAttackCooldown = __instance.m_attackCooldown;
                VanillaShootWhenAimDiff = __instance.m_shootWhenAimDiff;
                VanillaPredictionModifier = __instance.m_predictionModifier;
                VanillaTurnRate = __instance.m_turnRate;
                VanillaViewDistance = __instance.m_viewDistance;
                VanillaLookAcceleration = __instance.m_lookAcceleration;
                VanillaLookDeacceleration = __instance.m_lookDeacceleration;
                _vanillaCached = true;
                if (MegaQoLPlugin.DebugMode.Value)
                {
                    MegaQoLPlugin._logger.LogInfo($"[Ballista] Vanilla prefab values: cooldown={VanillaAttackCooldown}, shootWhenAimDiff={VanillaShootWhenAimDiff}, prediction={VanillaPredictionModifier}, turnRate={VanillaTurnRate}, viewDist={VanillaViewDistance}, lookAccel={VanillaLookAcceleration}, lookDecel={VanillaLookDeacceleration}");
                    BallistaFriendlyHelper.LogReflectionStatus();
                }
            }

            // Config values are live-applied every FixedUpdate via ApplyConfigValues.
            // Awake just handles friendly fire setup.
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            BallistaFriendlyHelper.ForceAntiPlayerTargeting(__instance);

            if (MegaQoLPlugin.DebugMode.Value)
                MegaQoLPlugin._logger.LogInfo($"[Ballista] Turret initialized: ammo={__instance.GetAmmo()}/{__instance.m_maxAmmo}, viewDist={__instance.m_viewDistance}, hAngle={__instance.m_horizontalAngle}");
        }
    }

    public static class BallistaFriendlyHelper
    {
        // Cached reflection — looked up ONCE, not every frame
        private static readonly FieldInfo _targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _haveTargetField = typeof(Turret).GetField("m_haveTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _allowedAmmoField = typeof(Turret).GetField("m_allowedAmmo", BindingFlags.Public | BindingFlags.Instance);
        private static readonly FieldInfo _updateTargetTimerField = typeof(Turret).GetField("m_updateTargetTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _aimDiffField = typeof(Turret).GetField("m_aimDiffToTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _nviewField = typeof(Turret).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance);

        public static FieldInfo TargetField => _targetField;
        public static FieldInfo HaveTargetField => _haveTargetField;
        public static FieldInfo AllowedAmmoField => _allowedAmmoField;

        public static void LogReflectionStatus()
        {
            if (MegaQoLPlugin.DebugMode.Value)
                MegaQoLPlugin._logger.LogInfo($"[Ballista] Reflection: m_target={(_targetField != null ? "OK" : "MISSING")}, m_haveTarget={(_haveTargetField != null ? "OK" : "MISSING")}, m_updateTargetTimer={(_updateTargetTimerField != null ? "OK" : "MISSING")}, m_aimDiffToTarget={(_aimDiffField != null ? "OK" : "MISSING")}, m_nview={(_nviewField != null ? "OK" : "MISSING")}, m_allowedAmmo={(_allowedAmmoField != null ? "OK" : "MISSING")}");
        }

        public static Character GetTarget(Turret turret)
        {
            if (_targetField == null) return null;
            return _targetField.GetValue(turret) as Character;
        }

        public static float GetAimDiff(Turret turret)
        {
            if (_aimDiffField == null) return -99f;
            return (float)_aimDiffField.GetValue(turret);
        }

        public static ZNetView GetNView(Turret turret)
        {
            if (_nviewField == null) return null;
            return _nviewField.GetValue(turret) as ZNetView;
        }

        public static bool IsFriendlyToPlayer(Character target)
        {
            if (target == null) return true;
            if (target is Player) return true;
            if (target.IsTamed()) return true;

            var baseAI = target.GetComponent<BaseAI>();
            if (baseAI != null)
            {
                Player player = Player.m_localPlayer;
                if (player != null && !baseAI.IsEnemy(player))
                    return true;
            }
            return false;
        }

        public static void ClearFriendlyTarget(Turret turret)
        {
            if (_targetField != null) _targetField.SetValue(turret, null);
            if (_haveTargetField != null) _haveTargetField.SetValue(turret, false);
            // Reset scan timer so turret re-scans for a hostile target immediately
            if (_updateTargetTimerField != null) _updateTargetTimerField.SetValue(turret, 0f);
        }

        public static void ForceAntiPlayerTargeting(Turret turret)
        {
            turret.m_targetPlayers = false;
            turret.m_targetTamed = false;
            turret.m_targetTamedConfig = false;
            turret.m_targetEnemies = true;
        }

        // Periodic diagnostic — one log every 3 seconds per turret to avoid spam
        private static float _diagTimer = 0f;
        public static void DiagnosticLog(Turret turret)
        {
            if (!MegaQoLPlugin.DebugMode.Value) return;
            _diagTimer += Time.fixedDeltaTime;
            if (_diagTimer < 3f) return;
            _diagTimer = 0f;

            var nview = GetNView(turret);
            bool isValid = nview != null && nview.IsValid();
            bool isOwner = isValid && nview.IsOwner();
            var target = GetTarget(turret);
            bool haveTarget = _haveTargetField != null && (bool)_haveTargetField.GetValue(turret);
            float aimDiff = GetAimDiff(turret);
            bool hasAmmo = turret.HasAmmo();
            int ammoCount = isValid ? nview.GetZDO().GetInt(ZDOVars.s_ammo, 0) : -1;
            bool cooling = turret.IsCoolingDown();
            string targetName = target != null ? $"{target.m_name}(tamed={target.IsTamed()},dead={target.IsDead()})" : "null";

            MegaQoLPlugin._logger.LogInfo(
                $"[Ballista-DIAG] isValid={isValid} isOwner={isOwner} | " +
                $"target={targetName} haveTarget={haveTarget} | " +
                $"aimDiff={aimDiff:F4} threshold={turret.m_shootWhenAimDiff:F4} aimOK={!(aimDiff < turret.m_shootWhenAimDiff)} | " +
                $"ammo={ammoCount}/{turret.m_maxAmmo} hasAmmo={hasAmmo} | " +
                $"cooling={cooling} cooldown={turret.m_attackCooldown:F1}s | " +
                $"flags: players={turret.m_targetPlayers} tamed={turret.m_targetTamed} tamedCfg={turret.m_targetTamedConfig} enemies={turret.m_targetEnemies}");
        }
    }

    [HarmonyPatch(typeof(Turret), "FixedUpdate")]
    public static class Turret_FixedUpdate_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Turret __instance)
        {
            // Live-apply config values every tick so changes take effect on existing turrets
            ApplyConfigValues(__instance);

            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            // Force targeting flags every tick in case vanilla/config resets them
            BallistaFriendlyHelper.ForceAntiPlayerTargeting(__instance);

            // Periodic diagnostic dump
            BallistaFriendlyHelper.DiagnosticLog(__instance);

            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                MegaQoLPlugin.Log($"[Ballista] FixedUpdate clearing friendly target: {target.m_name}");
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
            }
        }

        private static void ApplyConfigValues(Turret turret)
        {
            // FireRate: multiplier divides vanilla cooldown (1 = 2s, 10 = 0.2s)
            turret.m_attackCooldown = Turret_Awake_Patch.VanillaAttackCooldown / MegaQoLPlugin.BallistaFireRate.Value;

            // AimAccuracy: multiplier divides vanilla aim angle (1 = vanilla, 10 = 10x tighter)
            float vanillaAngleDeg = 2f * Mathf.Acos(Turret_Awake_Patch.VanillaShootWhenAimDiff) * Mathf.Rad2Deg;
            float tighterAngle = vanillaAngleDeg / MegaQoLPlugin.BallistaAimAccuracy.Value;
            turret.m_shootWhenAimDiff = Mathf.Cos(tighterAngle * Mathf.Deg2Rad * 0.5f);

            turret.m_turnRate = MegaQoLPlugin.BallistaTurnRate.Value;
            turret.m_viewDistance = MegaQoLPlugin.BallistaRange.Value;

            // Scale look acceleration/deceleration with turn rate ratio.
            // Vanilla has m_lookAcceleration=1.2, m_lookDeacceleration=0.05 at turnRate=45.
            // Without scaling these, the turret barrel lags behind fast-moving close targets
            // because smooth rotation can't keep up with the required angular velocity.
            float turnRatio = MegaQoLPlugin.BallistaTurnRate.Value / Turret_Awake_Patch.VanillaTurnRate;
            turret.m_lookAcceleration = Turret_Awake_Patch.VanillaLookAcceleration * turnRatio;
            turret.m_lookDeacceleration = Turret_Awake_Patch.VanillaLookDeacceleration * turnRatio;

            // Auto-compute prediction from velocity multiplier AND aim accuracy.
            // Vanilla prediction = 2 (deliberate 2x overshoot for game balance).
            // Perfect ballistic prediction = 1 (lead exactly one flight time).
            // AimAccuracy scales from vanilla overshoot toward physically perfect aim:
            //   aimAcc=1 → vanilla (2x overshoot), aimAcc=10 → perfect (1x lead)
            float velMultiplier = MegaQoLPlugin.BallistaVelocityMultiplier.Value;
            float aimAccuracy = MegaQoLPlugin.BallistaAimAccuracy.Value;
            float vanillaPred = Turret_Awake_Patch.VanillaPredictionModifier / velMultiplier;
            float perfectPred = 1f / velMultiplier;
            float t = (aimAccuracy - 1f) / 9f; // 0 at aim=1, 1 at aim=10
            turret.m_predictionModifier = Mathf.Lerp(vanillaPred, perfectPred, t);
        }
    }

    [HarmonyPatch(typeof(Turret), "UpdateTarget")]
    public static class Turret_UpdateTarget_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            BallistaFriendlyHelper.ForceAntiPlayerTargeting(__instance);
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                MegaQoLPlugin.Log($"[Ballista] UpdateTarget prefix clearing stale friendly: {target.m_name}");
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null)
            {
                if (BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                {
                    MegaQoLPlugin.Log($"[Ballista] UpdateTarget postfix rejecting friendly: {target.m_name} (tamed={target.IsTamed()})");
                    BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
                }
                else
                {
                    MegaQoLPlugin.Log($"[Ballista] UpdateTarget acquired hostile: {target.m_name}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(Turret), "UpdateAttack")]
    public static class Turret_UpdateAttack_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return true;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                MegaQoLPlugin.Log($"[Ballista] UpdateAttack blocking friendly: {target.m_name}");
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
                return false;
            }

            // Diagnostic: log each firing condition individually when target present but not firing
            if (MegaQoLPlugin.DebugMode.Value && target != null)
            {
                float aimDiff = BallistaFriendlyHelper.GetAimDiff(__instance);
                bool aimOK = !(aimDiff < __instance.m_shootWhenAimDiff);
                bool hasAmmo = __instance.HasAmmo();
                bool cooling = __instance.IsCoolingDown();
                if (!aimOK || !hasAmmo || cooling)
                {
                    MegaQoLPlugin._logger.LogInfo(
                        $"[Ballista] UpdateAttack NOT firing: target={target.m_name} aimDiff={aimDiff:F4} threshold={__instance.m_shootWhenAimDiff:F4} aimOK={aimOK} hasAmmo={hasAmmo} cooling={cooling}");
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Turret), "ShootProjectile")]
    public static class Turret_ShootProjectile_Patch
    {
        private static readonly FieldInfo _lastProjectileField = typeof(Turret).GetField("m_lastProjectile", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _projVelField = typeof(Projectile).GetField("m_vel", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        public static bool Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return true;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                MegaQoLPlugin.Log($"[Ballista] ShootProjectile blocking shot at friendly: {target.m_name}");
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
                return false;
            }
            if (MegaQoLPlugin.DebugMode.Value)
                MegaQoLPlugin._logger.LogInfo($"[Ballista] FIRING at {(target != null ? target.m_name : "unknown")} ammo={__instance.GetAmmo()}/{__instance.m_maxAmmo}");
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            if (_lastProjectileField == null || _projVelField == null) return;
            var projGO = _lastProjectileField.GetValue(__instance) as GameObject;
            if (projGO == null) return;
            var projectile = projGO.GetComponent<Projectile>();
            if (projectile == null) return;
            var vel = (Vector3)_projVelField.GetValue(projectile);
            if (vel.sqrMagnitude <= 0f) return;

            float speed = vel.magnitude;
            float velMultiplier = MegaQoLPlugin.BallistaVelocityMultiplier.Value;
            float finalSpeed = speed * Mathf.Max(1f, velMultiplier);
            float aimAccuracy = MegaQoLPlugin.BallistaAimAccuracy.Value;

            // Direct ballistic intercept: compute the mathematically perfect aim direction.
            // Vanilla aiming has multiple error sources (2D distance calc, parallax between
            // turret body and eye, smooth rotation lag, projectile spread). Instead of
            // patching each one, we compute the exact intercept point and redirect the bolt.
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (aimAccuracy > 1f && target != null && !target.IsDead())
            {
                Vector3 launchPos = __instance.m_eye.transform.position;
                // Aim at center-of-mass: feet position + half capsule height
                Vector3 targetPos = target.transform.position;
                CapsuleCollider capsule = target.GetComponentInChildren<CapsuleCollider>();
                float halfHeight = (capsule != null) ? capsule.height * 0.5f : 1f;
                targetPos.y += halfHeight;

                Vector3 targetVel = target.GetVelocity();
                Vector3 toTarget = targetPos - launchPos;
                float dist = toTarget.magnitude;

                // Solve intercept: |targetPos + targetVel*t - launchPos| = finalSpeed * t
                // Expanding: |toTarget + targetVel*t|² = (finalSpeed*t)²
                // a*t² + b*t + c = 0 where:
                float a = targetVel.sqrMagnitude - finalSpeed * finalSpeed;
                float b = 2f * Vector3.Dot(toTarget, targetVel);
                float c = toTarget.sqrMagnitude;

                float interceptTime = -1f;
                if (Mathf.Abs(a) < 0.001f)
                {
                    // Target speed ≈ bolt speed, linear solution
                    if (Mathf.Abs(b) > 0.001f)
                        interceptTime = -c / b;
                }
                else
                {
                    float disc = b * b - 4f * a * c;
                    if (disc >= 0f)
                    {
                        float sqrtDisc = Mathf.Sqrt(disc);
                        float t1 = (-b - sqrtDisc) / (2f * a);
                        float t2 = (-b + sqrtDisc) / (2f * a);
                        // Pick smallest positive solution
                        if (t1 > 0.01f && t2 > 0.01f) interceptTime = Mathf.Min(t1, t2);
                        else if (t1 > 0.01f) interceptTime = t1;
                        else if (t2 > 0.01f) interceptTime = t2;
                    }
                }

                if (interceptTime > 0f)
                {
                    Vector3 interceptPoint = targetPos + targetVel * interceptTime;
                    Vector3 perfectDir = (interceptPoint - launchPos).normalized;

                    // Blend between barrel direction and perfect intercept based on AimAccuracy
                    // aim=1: 100% barrel (vanilla), aim=10: 100% perfect intercept
                    float blendT = Mathf.Clamp01((aimAccuracy - 1f) / 9f);
                    Vector3 barrelDir = __instance.m_eye.transform.forward;
                    Vector3 finalDir = Vector3.Slerp(barrelDir, perfectDir, blendT).normalized;

                    _projVelField.SetValue(projectile, finalDir * finalSpeed);
                    return;
                }
            }

            // Fallback: just apply velocity multiplier with barrel direction
            Vector3 fallbackDir = __instance.m_eye.transform.forward;
            _projVelField.SetValue(projectile, fallbackDir * finalSpeed);
        }
    }

    // (Build Dust Removal & Rune Build patches moved to MegaBuilder v1.5.0+)

    // ==================== NO MIST ====================

    [HarmonyPatch(typeof(MistEmitter), "SetEmit")]
    public static class MistEmitter_SetEmit_Patch
    {
        private static readonly FieldInfo _emitField = AccessTools.Field(typeof(MistEmitter), "m_emit");

        [HarmonyPrefix]
        public static void Prefix(MistEmitter __instance)
        {
            if (MegaQoLPlugin.EnableNoMist.Value)
                _emitField.SetValue(__instance, false);
        }
    }

    [HarmonyPatch(typeof(MistEmitter), "Update")]
    public static class MistEmitter_Update_Patch
    {
        private static readonly FieldInfo _emitField = AccessTools.Field(typeof(MistEmitter), "m_emit");

        [HarmonyPrefix]
        public static void Prefix(MistEmitter __instance)
        {
            if (MegaQoLPlugin.EnableNoMist.Value)
                _emitField.SetValue(__instance, false);
        }
    }

    [HarmonyPatch(typeof(ParticleMist), "Awake")]
    public static class ParticleMist_Awake_Patch
    {
        private static readonly FieldInfo _psField = AccessTools.Field(typeof(ParticleMist), "m_ps");

        [HarmonyPostfix]
        public static void Postfix(ParticleMist __instance)
        {
            if (__instance == null) return;
            var ps = _psField.GetValue(__instance) as ParticleSystem;
            if (ps == null) return;
            if (MegaQoLPlugin.EnableNoMist.Value)
            {
                var emission = ps.emission;
                emission.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(ParticleMist), "Update")]
    public static class ParticleMist_Update_Patch
    {
        private static readonly FieldInfo _psField = AccessTools.Field(typeof(ParticleMist), "m_ps");

        [HarmonyPrefix]
        public static void Prefix(ParticleMist __instance)
        {
            if (__instance == null) return;
            var ps = _psField.GetValue(__instance) as ParticleSystem;
            if (ps == null) return;
            var emission = ps.emission;
            emission.enabled = !MegaQoLPlugin.EnableNoMist.Value;
        }
    }

    // ==================== MESSAGEHUD SMART QUEUE PATCH ====================

    [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
    public static class MessageHud_ShowMessage_OverwritePickupPatch
    {
        private static FieldInfo _queueField = typeof(MessageHud).GetField("m_msgQeue", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _timerField = typeof(MessageHud).GetField("m_msgQueueTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _clearMethod;

        [HarmonyPrefix]
        public static void Prefix(MessageHud __instance, MessageHud.MessageType type)
        {
            if (!MegaQoLPlugin.EnableMessageHudQueue.Value) return;
            if (type != MessageHud.MessageType.TopLeft) return;
            if (_queueField == null) return;

            var queue = _queueField.GetValue(__instance);
            if (queue != null)
            {
                if (_clearMethod == null)
                    _clearMethod = queue.GetType().GetMethod("Clear");
                if (_clearMethod != null)
                    _clearMethod.Invoke(queue, null);
            }

            if (_timerField != null)
                _timerField.SetValue(__instance, 999f);
        }
    }

    // ==================== AOE MINING ====================

    /// <summary>
    /// Deferred destroy for MineRock5/MineRock — calling Damage() from a Harmony
    /// Postfix loses RPCs so drops never spawn. Deferring 2 frames runs on a clean
    /// stack where RPCs process normally.
    /// </summary>
    public class DeferredMineRockDestroy : MonoBehaviour
    {
        private static MethodInfo _damageAreaMethod;
        private static FieldInfo _hitAreasField;
        private static FieldInfo _colliderField;

        private int frameDelay = 2;

        public void Setup(Vector3 impact)
        {
        }

        void Update()
        {
            if (frameDelay-- > 0) return;

            try
            {
                var rock5 = GetComponent<MineRock5>();
                if (rock5 != null) { DestroyRock5(rock5); Destroy(this); return; }

                var rock = GetComponent<MineRock>();
                if (rock != null)
                {
                    rock.Damage(CreateDestroyHit(rock.transform.position));
                    Destroy(this);
                    return;
                }
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }

            Destroy(this);
        }

        private void DestroyRock5(MineRock5 rock)
        {
            try
            {
                var nview = rock.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid() && !nview.IsOwner())
                    nview.ClaimOwnership();
            }
            catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }

            if (_damageAreaMethod == null)
                _damageAreaMethod = typeof(MineRock5).GetMethod("DamageArea",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_damageAreaMethod == null) return;

            if (_hitAreasField == null)
                _hitAreasField = typeof(MineRock5).GetField("m_hitAreas",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_hitAreasField == null) return;

            var hitAreas = _hitAreasField.GetValue(rock) as System.Collections.IList;
            if (hitAreas == null || hitAreas.Count == 0) return;

            if (_colliderField == null)
            {
                Type hitAreaType = hitAreas[0].GetType();
                _colliderField = hitAreaType.GetField("m_collider",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            for (int i = 0; i < hitAreas.Count; i++)
            {
                var area = hitAreas[i];
                if (area == null) continue;

                Collider col = _colliderField?.GetValue(area) as Collider;
                if (col == null || !col.enabled) continue;

                HitData areaHit = CreateDestroyHit(col.bounds.center);
                try { _damageAreaMethod.Invoke(rock, new object[] { i, areaHit }); }
                catch (Exception ex) { MegaQoLPlugin._logger?.LogDebug($"[MegaQoL] {ex.Message}"); }
            }
        }

        private static HitData CreateDestroyHit(Vector3 point)
        {
            HitData hit = new HitData();
            hit.m_point = point;
            hit.m_damage.m_damage = 999999f;
            hit.m_damage.m_pickaxe = 999999f;
            hit.m_toolTier = 9999;
            return hit;
        }
    }

    public static class AOEMiningHelper
    {
        // Per-frame lock: only one object gets instant-mined per swing
        private static int _lastMinedFrame = -1;

        public static bool TryClaimFrame()
        {
            int frame = Time.frameCount;
            if (frame == _lastMinedFrame) return false;
            _lastMinedFrame = frame;
            return true;
        }
    }

    // Harmony patches: instant-mine when pickaxe hits with hotkey held
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    public static class AOEMining_MineRock5_Patch
    {
        [HarmonyPostfix]
        static void Postfix(MineRock5 __instance, HitData hit)
        {
            if (!MegaQoLPlugin.EnableAOEMining.Value) return;
            if (!Input.GetKey(MegaQoLPlugin.AOEMiningKey.Value)) return;
            if (hit.m_skill != Skills.SkillType.Pickaxes) return;
            if (!AOEMiningHelper.TryClaimFrame()) return;

            if (MegaQoLPlugin.DebugMode.Value)
                MegaQoLPlugin._logger.LogInfo($"[InstantMine] MineRock5 '{__instance.name}' — attaching deferred destroy");

            if (__instance.GetComponent<DeferredMineRockDestroy>() == null)
                __instance.gameObject.AddComponent<DeferredMineRockDestroy>().Setup(hit.m_point);
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    public static class AOEMining_MineRock_Patch
    {
        [HarmonyPostfix]
        static void Postfix(MineRock __instance, HitData hit)
        {
            if (!MegaQoLPlugin.EnableAOEMining.Value) return;
            if (!Input.GetKey(MegaQoLPlugin.AOEMiningKey.Value)) return;
            if (hit.m_skill != Skills.SkillType.Pickaxes) return;
            if (!AOEMiningHelper.TryClaimFrame()) return;

            if (MegaQoLPlugin.DebugMode.Value)
                MegaQoLPlugin._logger.LogInfo($"[InstantMine] MineRock '{__instance.name}' — attaching deferred destroy");

            if (__instance.GetComponent<DeferredMineRockDestroy>() == null)
                __instance.gameObject.AddComponent<DeferredMineRockDestroy>().Setup(hit.m_point);
        }
    }

    // ==================== ZNetScene NRE-Trap (always on) ====================
    //
    // Vanilla ZNetScene.RemoveObjects can NRE every frame when m_instances has a
    // null/destroyed ZNetView entry — typically left there by another mod that
    // Object.Destroy'd a ZNetView without removing its dictionary entry. The NRE
    // breaks the cleanup pass, which makes gameplay glitchy (dungeon objects
    // persist after teleport, lighting/audio desync, etc).
    //
    // This finalizer catches the NRE, sweeps m_instances for null/destroyed
    // entries, logs the offender (throttled), removes them, and swallows the
    // exception so Update() keeps ticking. Always-on — only does work if the
    // NRE actually fires.
    [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
    public static class ZNetScene_RemoveObjects_NRETrap
    {
        private static int _sweepCount;
        private static int _logCount;
        private const int LogLimit = 25;

        // ZNetScene's per-instance ZDO→ZNetView map is private; resolve via reflection
        // once and cache. Field name is stable across recent Valheim versions.
        private static FieldInfo _instancesField;
        private static FieldInfo InstancesField =>
            _instancesField ??= AccessTools.Field(typeof(ZNetScene), "m_instances");

        [HarmonyFinalizer]
        static Exception Finalizer(ZNetScene __instance, Exception __exception)
        {
            if (__exception == null) return null;
            if (!(__exception is NullReferenceException)) return __exception;

            try
            {
                int removed = SweepNullEntries(__instance);
                _sweepCount++;
                if (_logCount < LogLimit)
                {
                    _logCount++;
                    MegaQoLPlugin._logger.LogError(
                        $"[ZNetScene-NRETrap] Caught NRE in RemoveObjects (sweep #{_sweepCount}); cleared {removed} dud entries from m_instances." +
                        (_logCount == LogLimit ? $" (Throttled — further sweep notices suppressed this session.)" : string.Empty));
                }
            }
            catch (Exception sweepErr)
            {
                MegaQoLPlugin._logger.LogError($"[ZNetScene-NRETrap] sweep failed: {sweepErr}");
            }
            return null;
        }

        private static int SweepNullEntries(ZNetScene zns)
        {
            var field = InstancesField;
            if (field == null) return 0;

            var dict = field.GetValue(zns) as IDictionary<ZDO, ZNetView>;
            if (dict == null) return 0;

            var removeKeys = new List<ZDO>();
            foreach (var kv in dict)
            {
                var view = kv.Value;
                if (view == null)
                {
                    removeKeys.Add(kv.Key);
                    LogOffender(zns, kv.Key, viewName: "<null ZNetView>");
                    continue;
                }
                if (view.gameObject == null)
                {
                    removeKeys.Add(kv.Key);
                    LogOffender(zns, kv.Key, viewName: $"<destroyed gameObject> (view='{SafeName(view)}')");
                    continue;
                }
            }
            foreach (var k in removeKeys) dict.Remove(k);
            return removeKeys.Count;
        }

        private static void LogOffender(ZNetScene zns, ZDO zdo, string viewName)
        {
            if (_logCount >= LogLimit) return;
            _logCount++;

            string zdoUid = zdo != null ? zdo.m_uid.ToString() : "<null ZDO>";
            int prefabHash = 0;
            string prefabName = "<unresolved>";
            try
            {
                if (zdo != null) prefabHash = zdo.GetPrefab();
                if (prefabHash != 0)
                {
                    var prefabGo = zns?.GetPrefab(prefabHash);
                    if (prefabGo != null) prefabName = prefabGo.name;
                    else prefabName = $"<unknown hash={prefabHash}>";
                }
            }
            catch { /* best-effort identification */ }

            MegaQoLPlugin._logger.LogError(
                $"[ZNetScene-NRETrap]   dud entry: zdoUid={zdoUid} prefab='{prefabName}' state={viewName}");
        }

        private static string SafeName(UnityEngine.Object obj)
        {
            try { return obj != null ? obj.name : "<null>"; } catch { return "<exception>"; }
        }
    }

    // ==================== CONFIG PRUNER (orphan section/key sweeper) ====================

    /// <summary>
    /// Removes sections and keys from the on-disk cfg that aren't bound to any
    /// `ConfigEntry` on the supplied `ConfigFile`. Run AFTER every `Bind()` call
    /// completes — that's when `cfg.Keys` is the full snapshot of the mod's
    /// current surface area.
    ///
    /// Why: BepInEx's cfg writer never deletes orphan content. When a key is
    /// renamed, a section reorganised, or a feature extracted into a different
    /// mod, the old text persists forever and profiles snowball across versions.
    /// </summary>
    public static class ConfigPruner
    {
        public static int Prune(ConfigFile cfg, ManualLogSource log = null)
        {
            if (cfg == null) return 0;
            string path = cfg.ConfigFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;

            var bound = new HashSet<string>(StringComparer.Ordinal);
            var boundSections = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (var def in cfg.Keys)
                {
                    bound.Add(def.Section + "\0" + def.Key);
                    boundSections.Add(def.Section);
                }
            }
            catch (Exception ex)
            {
                log?.LogWarning($"[ConfigPruner] Couldn't enumerate bound entries: {ex.Message}");
                return 0;
            }
            if (boundSections.Count == 0) return 0;

            string[] inLines;
            try { inLines = File.ReadAllLines(path); }
            catch (Exception ex)
            {
                log?.LogWarning($"[ConfigPruner] Read failed: {ex.Message}");
                return 0;
            }

            var outLines = new List<string>(inLines.Length);
            string currentSection = null;
            bool currentSectionKeep = true;
            int droppedSections = 0, droppedKeys = 0;

            for (int i = 0; i < inLines.Length; i++)
            {
                var line = inLines[i];
                var trimmed = line.Trim();

                if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[trimmed.Length - 1] == ']')
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    currentSectionKeep = boundSections.Contains(currentSection);
                    if (!currentSectionKeep)
                    {
                        droppedSections++;
                        TrimTrailingBlanksAndComments(outLines);
                        continue;
                    }
                    outLines.Add(line);
                    continue;
                }

                if (!currentSectionKeep) continue;

                int eq = trimmed.IndexOf('=');
                bool isKeyLine = eq > 0 && trimmed[0] != '#' && trimmed[0] != ';';
                if (isKeyLine && currentSection != null)
                {
                    string key = trimmed.Substring(0, eq).Trim();
                    if (!bound.Contains(currentSection + "\0" + key))
                    {
                        droppedKeys++;
                        TrimTrailingBlanksAndComments(outLines);
                        continue;
                    }
                }
                outLines.Add(line);
            }

            if (droppedSections == 0 && droppedKeys == 0) return 0;

            while (outLines.Count > 0 && outLines[outLines.Count - 1].Trim().Length == 0)
                outLines.RemoveAt(outLines.Count - 1);
            outLines.Add(string.Empty);

            try
            {
                File.WriteAllLines(path, outLines);
                log?.LogInfo($"[ConfigPruner] {Path.GetFileName(path)}: dropped {droppedSections} orphan section(s), {droppedKeys} orphan key(s)");
            }
            catch (Exception ex)
            {
                log?.LogWarning($"[ConfigPruner] Write failed: {ex.Message}");
                return 0;
            }
            return droppedSections + droppedKeys;
        }

        private static void TrimTrailingBlanksAndComments(List<string> lines)
        {
            while (lines.Count > 0)
            {
                var last = lines[lines.Count - 1].TrimStart();
                if (last.Length == 0 || last[0] == '#' || last[0] == ';')
                    lines.RemoveAt(lines.Count - 1);
                else
                    break;
            }
        }
    }
}
