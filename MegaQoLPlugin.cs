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
        public const string PluginVersion = "1.0.0";

        private static ManualLogSource _logger;
        private static Harmony _harmony;
        private static ConfigFile _config;
        private static FileSystemWatcher _configWatcher;

        // Auto Repair
        public static ConfigEntry<bool> EnableAutoRepair;
        public static ConfigEntry<KeyCode> AutoRepairToggleKey;
        public static ConfigEntry<float> AutoRepairRange;
        public static ConfigEntry<float> AutoRepairInterval;

        // Refueller
        public static ConfigEntry<bool> EnableAutoRefuel;
        public static ConfigEntry<float> AutoRefuelRange;
        public static ConfigEntry<float> AutoRefuelInterval;

        // Ballista Reloader
        public static ConfigEntry<bool> EnableBallistaAutoReload;
        public static ConfigEntry<float> BallistaAutoReloadRange;
        public static ConfigEntry<float> BallistaAutoReloadInterval;
        public static ConfigEntry<bool> BallistaAutoReloadUseChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseReinforcedChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseBlackMetalChests;
        public static ConfigEntry<bool> BallistaAutoReloadUseBarrels;

        // Pet Feeder
        public static ConfigEntry<bool> EnableAutoPetFeeder;
        public static ConfigEntry<float> AutoPetFeederRange;
        public static ConfigEntry<float> AutoPetFeederInterval;
        public static ConfigEntry<bool> AutoPetFeederUseChests;
        public static ConfigEntry<bool> AutoPetFeederUseReinforcedChests;
        public static ConfigEntry<bool> AutoPetFeederUseBlackMetalChests;
        public static ConfigEntry<bool> AutoPetFeederUseBarrels;

        // Item Management
        public static ConfigEntry<bool> EnablePlayerPickupRadius;
        public static ConfigEntry<float> PlayerPickupRadius;
        public static ConfigEntry<bool> EnableChestAutoPickup;
        public static ConfigEntry<float> ChestAutoPickupRadius;
        public static ConfigEntry<float> ChestAutoPickupInterval;
        public static ConfigEntry<KeyCode> QuickDepositKey;
        public static ConfigEntry<float> QuickDepositRadius;

        // Craft from Containers
        public static ConfigEntry<bool> EnableCraftFromContainers;
        public static ConfigEntry<float> CraftFromContainersRadius;

        // Map Teleport
        public static ConfigEntry<bool> EnableMapTeleport;

        // Ballista Improvements
        public static ConfigEntry<bool> EnableBallistaImprovements;

        // Plant Anywhere
        public static ConfigEntry<bool> EnablePlantAnywhere;

        // Build Dust Removal
        public static ConfigEntry<bool> EnableNoBuildDust;

        // Timers
        private static float _autoRefuelTimer = 0f;
        private static float _autoPetFeederTimer = 0f;
        private static float _ballistaAutoReloadTimer = 0f;
        private static float _autoRepairTimer = 0f;
        private static float _chestAutoPickupTimer = 0f;
        public static bool AutoRepairActive = false;

        private void Awake()
        {
            _logger = Logger;
            _logger.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // 1. Auto Repair
            EnableAutoRepair = Config.Bind("1. Auto Repair", "Enable", true,
                "Enables auto-repair - press toggle key to automatically repair all nearby build pieces");
            AutoRepairToggleKey = Config.Bind("1. Auto Repair", "ToggleKey", KeyCode.LeftBracket,
                "Key to toggle auto-repair on/off (default: Left Bracket '[' key)");
            AutoRepairRange = Config.Bind("1. Auto Repair", "Range", 30f,
                new ConfigDescription("Range for auto-repair in meters", new AcceptableValueRange<float>(1f, 100f)));
            AutoRepairInterval = Config.Bind("1. Auto Repair", "Interval", 3f,
                new ConfigDescription("How often to repair nearby pieces (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));

            // 2. Refueller
            EnableAutoRefuel = Config.Bind("2. Refueller", "Enable", true,
                "Automatically refuel nearby fire sources (campfires, torches, braziers, etc)");
            AutoRefuelRange = Config.Bind("2. Refueller", "Range", 10f,
                new ConfigDescription("Range for auto-refueling fires in meters", new AcceptableValueRange<float>(0f, 100f)));
            AutoRefuelInterval = Config.Bind("2. Refueller", "Interval", 2f,
                new ConfigDescription("How often to check for fires to refuel (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));

            // 3. Ballista Reloader
            EnableBallistaAutoReload = Config.Bind("3. Ballista Reloader", "Enable", true,
                "Automatically reload ballistas from nearby containers");
            BallistaAutoReloadRange = Config.Bind("3. Ballista Reloader", "Range", 10f,
                new ConfigDescription("Range to search for containers in meters", new AcceptableValueRange<float>(0f, 100f)));
            BallistaAutoReloadInterval = Config.Bind("3. Ballista Reloader", "Interval", 5f,
                new ConfigDescription("How often to reload ballistas (seconds)", new AcceptableValueRange<float>(1f, 60f)));
            BallistaAutoReloadUseChests = Config.Bind("3. Ballista Reloader", "UseChests", true,
                "Allow reloading from regular Chests");
            BallistaAutoReloadUseReinforcedChests = Config.Bind("3. Ballista Reloader", "UseReinforcedChests", true,
                "Allow reloading from Reinforced Chests");
            BallistaAutoReloadUseBlackMetalChests = Config.Bind("3. Ballista Reloader", "UseBlackMetalChests", true,
                "Allow reloading from Black Metal Chests");
            BallistaAutoReloadUseBarrels = Config.Bind("3. Ballista Reloader", "UseBarrels", true,
                "Allow reloading from Barrels");

            // 4. Pet Feeder
            EnableAutoPetFeeder = Config.Bind("4. Pet Feeder", "Enable", true,
                "Automatically feeds tamed creatures from nearby containers");
            AutoPetFeederRange = Config.Bind("4. Pet Feeder", "Range", 10f,
                new ConfigDescription("Range to search for containers in meters", new AcceptableValueRange<float>(0f, 100f)));
            AutoPetFeederInterval = Config.Bind("4. Pet Feeder", "Interval", 10f,
                new ConfigDescription("How often to feed pets (seconds)", new AcceptableValueRange<float>(1f, 60f)));
            AutoPetFeederUseChests = Config.Bind("4. Pet Feeder", "UseChests", true,
                "Allow feeding from regular Chests");
            AutoPetFeederUseReinforcedChests = Config.Bind("4. Pet Feeder", "UseReinforcedChests", true,
                "Allow feeding from Reinforced Chests");
            AutoPetFeederUseBlackMetalChests = Config.Bind("4. Pet Feeder", "UseBlackMetalChests", true,
                "Allow feeding from Black Metal Chests");
            AutoPetFeederUseBarrels = Config.Bind("4. Pet Feeder", "UseBarrels", true,
                "Allow feeding from Barrels");

            // 5. Item Management
            EnablePlayerPickupRadius = Config.Bind("5. Item Management", "EnablePlayerPickupRadius", true,
                "Enables configurable player item pickup radius");
            PlayerPickupRadius = Config.Bind("5. Item Management", "PlayerPickupRadius", 2f,
                new ConfigDescription("Player item auto-pickup radius in meters (vanilla = 2)", new AcceptableValueRange<float>(1f, 50f)));
            EnableChestAutoPickup = Config.Bind("5. Item Management", "EnableChestAutoPickup", false,
                "Chests automatically pull in nearby ground items that match their contents");
            ChestAutoPickupRadius = Config.Bind("5. Item Management", "ChestAutoPickupRadius", 10f,
                new ConfigDescription("Radius around chests to pull in matching ground items", new AcceptableValueRange<float>(1f, 100f)));
            ChestAutoPickupInterval = Config.Bind("5. Item Management", "ChestAutoPickupInterval", 2f,
                new ConfigDescription("How often chests check for nearby items (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));
            QuickDepositKey = Config.Bind("5. Item Management", "QuickDepositKey", KeyCode.Period,
                "Hotkey to deposit matching items from inventory into nearby chests");
            QuickDepositRadius = Config.Bind("5. Item Management", "QuickDepositRadius", 10f,
                new ConfigDescription("Radius to search for chests when quick-depositing", new AcceptableValueRange<float>(1f, 100f)));

            // 6. Craft from Containers
            EnableCraftFromContainers = Config.Bind("6. Craft from Containers", "Enable", true,
                "Allows crafting stations to pull materials from nearby containers");
            CraftFromContainersRadius = Config.Bind("6. Craft from Containers", "Radius", 10f,
                new ConfigDescription("Radius to search for containers when crafting", new AcceptableValueRange<float>(1f, 100f)));

            // 7. Map Teleport
            EnableMapTeleport = Config.Bind("7. Map Teleport", "Enable", true,
                "Enables map teleportation - middle-click on map to teleport to that location");

            // 8. Ballista Improvements
            EnableBallistaImprovements = Config.Bind("8. Ballista Improvements", "Enable", true,
                "Enables ballista friendly-fire prevention (won't shoot players or tamed creatures)");

            // 9. Plant Anywhere
            EnablePlantAnywhere = Config.Bind("9. Plant Anywhere", "Enable", true,
                "Enables planting crops in any biome (removes biome restrictions for non-tree plantables)");

            // 10. Build Dust Removal
            EnableNoBuildDust = Config.Bind("10. Build Dust Removal", "Enable", true,
                "Removes dust/particle effects when placing build pieces (keeps sound effects)");

            _config = Config;
            SetupConfigWatcher();

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            _logger.LogInfo($"{PluginName} loaded successfully!");
            _logger.LogInfo($"Live config reloading enabled - edit {Config.ConfigFilePath} and save to apply changes!");
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

            _logger.LogInfo($"Config watcher started for: {configFile}");
        }

        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(100);
                _config.Reload();
                _logger.LogInfo("Config reloaded! Changes applied.");

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

        public static void Log(string message) => _logger?.LogInfo(message);
        public static void LogWarning(string message) => _logger?.LogWarning(message);
        public static void LogError(string message) => _logger?.LogError(message);

        private static bool IsUIBlockingInput()
        {
            try
            {
                if (InventoryGui.IsVisible()) return true;
                if (Menu.IsVisible()) return true;
                if (Minimap.IsOpen()) return true;
                if (Console.IsVisible()) return true;
                if (TextInput.IsVisible()) return true;
                if (StoreGui.IsVisible()) return true;
                if (Hud.IsPieceSelectionVisible()) return true;
                if (Chat.instance != null && Chat.instance.HasFocus()) return true;
                if (Player.m_localPlayer != null && Player.m_localPlayer.InPlaceMode()) return true;
                if (TextViewer.instance != null && TextViewer.instance.IsVisible()) return true;
            }
            catch { }
            return false;
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            // Auto-repair toggle
            if (EnableAutoRepair.Value && Input.GetKeyDown(AutoRepairToggleKey.Value) && !IsUIBlockingInput())
            {
                AutoRepairActive = !AutoRepairActive;
                player.Message(MessageHud.MessageType.Center, AutoRepairActive ? "Auto Repair: ON" : "Auto Repair: OFF");
            }

            if (AutoRepairActive && !EnableAutoRepair.Value)
                AutoRepairActive = false;

            // Auto-repair nearby build pieces
            if (AutoRepairActive)
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

            // Auto pet feeder
            if (EnableAutoPetFeeder.Value)
            {
                _autoPetFeederTimer += Time.deltaTime;
                if (_autoPetFeederTimer >= AutoPetFeederInterval.Value)
                {
                    _autoPetFeederTimer = 0f;
                    AutoPetFeederHelper.FeedNearbyPets(player.transform.position, AutoPetFeederRange.Value);
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

            // Chest auto-pickup
            if (EnableChestAutoPickup.Value)
            {
                _chestAutoPickupTimer += Time.deltaTime;
                if (_chestAutoPickupTimer >= ChestAutoPickupInterval.Value)
                {
                    _chestAutoPickupTimer = 0f;
                    ChestAutoPickupHelper.PickupNearbyItems(player.transform.position, ChestAutoPickupRadius.Value);
                }
            }

            // Quick deposit hotkey
            if (Input.GetKeyDown(QuickDepositKey.Value) && !IsUIBlockingInput())
            {
                QuickDepositHelper.DepositMatchingItems(player, QuickDepositRadius.Value);
            }
        }
    }

    // ==================== AUTO REPAIR ====================

    public static class AutoRepairHelper
    {
        public static void RepairNearbyPieces(Vector3 position, float range)
        {
            var allInstances = WearNTear.GetAllInstances();
            if (allInstances == null) return;

            foreach (var wnt in allInstances)
            {
                if (wnt == null) continue;
                if (Vector3.Distance(position, wnt.transform.position) > range) continue;
                if (wnt.GetHealthPercentage() >= 1f) continue;
                wnt.Repair();
            }
        }
    }

    // ==================== AUTO-REFUEL ====================

    public static class AutoRefuelHelper
    {
        public static void RefuelNearbyFireSources(Vector3 position, float range)
        {
            #pragma warning disable CS0618
            var fireplaces = UnityEngine.Object.FindObjectsOfType<Fireplace>();
            #pragma warning restore CS0618

            foreach (var fireplace in fireplaces)
            {
                if (fireplace == null) continue;
                if (Vector3.Distance(position, fireplace.transform.position) > range) continue;
                RefuelFireplace(fireplace);
            }

            #pragma warning disable CS0618
            var cookingStations = UnityEngine.Object.FindObjectsOfType<CookingStation>();
            #pragma warning restore CS0618
            foreach (var station in cookingStations)
            {
                if (station == null) continue;
                if (Vector3.Distance(position, station.transform.position) > range) continue;

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
                if (currentFuel < maxFuel * 0.8f)
                    nview.GetZDO().Set(ZDOVars.s_fuel, maxFuel);
            }
            catch { }
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
                if (currentFuel < maxFuel * 0.8f)
                    nview.GetZDO().Set(ZDOVars.s_fuel, maxFuel);
            }
            catch { }
        }
    }

    // ==================== AUTO PET FEEDER ====================

    public static class AutoPetFeederHelper
    {
        public static void FeedNearbyPets(Vector3 position, float range)
        {
            var allCharacters = Character.GetAllCharacters();

            foreach (var character in allCharacters)
            {
                if (character == null) continue;
                if (!character.IsTamed()) continue;
                if (character is Player) continue;

                float distance = Vector3.Distance(position, character.transform.position);
                if (distance > range) continue;

                var tameable = character.GetComponent<Tameable>();
                if (tameable == null) continue;

                TryFeedCreature(tameable, position, range);
            }
        }

        private static void TryFeedCreature(Tameable tameable, Vector3 position, float range)
        {
            var monsterAI = tameable.GetComponent<MonsterAI>();
            if (monsterAI == null) return;

            var consumeItemsField = typeof(MonsterAI).GetField("m_consumeItems", BindingFlags.Public | BindingFlags.Instance);
            if (consumeItemsField == null) return;

            var consumeItems = consumeItemsField.GetValue(monsterAI) as List<ItemDrop>;
            if (consumeItems == null || consumeItems.Count == 0) return;

            var validFoodNames = new List<string>();
            foreach (var foodItem in consumeItems)
            {
                if (foodItem != null)
                    validFoodNames.Add(foodItem.gameObject.name);
            }

            var nview = tameable.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;

            if (!tameable.IsHungry()) return;

            #pragma warning disable CS0618
            var containers = UnityEngine.Object.FindObjectsOfType<Container>();
            #pragma warning restore CS0618

            foreach (var container in containers)
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
                    string prefabName = item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name;

                    if (validFoodNames.Contains(prefabName))
                    {
                        inventory.RemoveItem(item, 1);
                        var onConsumedMethod = typeof(Tameable).GetMethod("OnConsumedItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (onConsumedMethod != null)
                            onConsumedMethod.Invoke(tameable, new object[] { null });
                        return;
                    }
                }
            }
        }

        private static bool IsAllowedContainer(Container container)
        {
            string name = container.gameObject.name.ToLower();
            if (name.Contains("piece_chest_blackmetal") || name.StartsWith("blackmetalchest"))
                return MegaQoLPlugin.AutoPetFeederUseBlackMetalChests.Value;
            if (name.Contains("barrel"))
                return MegaQoLPlugin.AutoPetFeederUseBarrels.Value;
            if ((name.Contains("piece_chest") || name.StartsWith("reinforcedchest")) &&
                !name.Contains("wood") && !name.Contains("blackmetal") && !name.Contains("private"))
                return MegaQoLPlugin.AutoPetFeederUseReinforcedChests.Value;
            if (name.Contains("piece_chest_wood") || name.StartsWith("chest(") || (name.Contains("chest") && name.Contains("wood")))
                return MegaQoLPlugin.AutoPetFeederUseChests.Value;
            if (name.Contains("private"))
                return false;
            return false;
        }
    }

    // ==================== BALLISTA AUTO RELOAD ====================

    public static class BallistaAutoReloadHelper
    {
        public static void ReloadNearbyBallistas(Vector3 position, float range)
        {
            #pragma warning disable CS0618
            var turrets = UnityEngine.Object.FindObjectsOfType<Turret>();
            #pragma warning restore CS0618

            foreach (var turret in turrets)
            {
                if (turret == null) continue;
                if (Vector3.Distance(position, turret.transform.position) > range) continue;

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

            var allowedAmmoField = typeof(Turret).GetField("m_allowedAmmo", BindingFlags.Public | BindingFlags.Instance);
            List<ItemDrop> allowedAmmo = null;
            if (allowedAmmoField != null)
                allowedAmmo = allowedAmmoField.GetValue(turret) as List<ItemDrop>;

            var validAmmoNames = new List<string>();
            if (allowedAmmo != null && allowedAmmo.Count > 0)
            {
                foreach (var ammo in allowedAmmo)
                    if (ammo != null) validAmmoNames.Add(ammo.gameObject.name);
            }
            else
            {
                validAmmoNames.AddRange(new[] { "TurretBolt", "TurretBoltWood", "TurretBoltFlametal" });
            }

            #pragma warning disable CS0618
            var containers = UnityEngine.Object.FindObjectsOfType<Container>();
            #pragma warning restore CS0618

            foreach (var container in containers)
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
            string name = container.gameObject.name.ToLower();
            if (name.Contains("piece_chest_blackmetal") || name.StartsWith("blackmetalchest"))
                return MegaQoLPlugin.BallistaAutoReloadUseBlackMetalChests.Value;
            if (name.Contains("barrel"))
                return MegaQoLPlugin.BallistaAutoReloadUseBarrels.Value;
            if ((name.Contains("piece_chest") || name.StartsWith("reinforcedchest")) &&
                !name.Contains("wood") && !name.Contains("blackmetal") && !name.Contains("private"))
                return MegaQoLPlugin.BallistaAutoReloadUseReinforcedChests.Value;
            if (name.Contains("piece_chest_wood") || name.StartsWith("chest(") || (name.Contains("chest") && name.Contains("wood")))
                return MegaQoLPlugin.BallistaAutoReloadUseChests.Value;
            if (name.Contains("private"))
                return false;
            return false;
        }
    }

    // ==================== PLAYER PICKUP RADIUS ====================

    [HarmonyPatch(typeof(Player), "AutoPickup")]
    public static class Player_AutoPickup_Patch
    {
        private static float _savedRange;

        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            if (!MegaQoLPlugin.EnablePlayerPickupRadius.Value) return;
            if (__instance != Player.m_localPlayer) return;

            _savedRange = __instance.m_autoPickupRange;
            __instance.m_autoPickupRange = MegaQoLPlugin.PlayerPickupRadius.Value;
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (!MegaQoLPlugin.EnablePlayerPickupRadius.Value) return;
            if (__instance != Player.m_localPlayer) return;

            __instance.m_autoPickupRange = _savedRange;
        }
    }

    // ==================== CHEST AUTO-PICKUP ====================

    public static class ChestAutoPickupHelper
    {
        private static readonly Collider[] _overlapBuffer = new Collider[128];

        public static void PickupNearbyItems(Vector3 playerPosition, float radius)
        {
            var nearbyContainers = new List<Container>();
            var seen = new HashSet<int>();
            int hitCount = Physics.OverlapSphereNonAlloc(playerPosition, radius, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var container = _overlapBuffer[i].GetComponentInParent<Container>();
                if (container == null) continue;
                int id = container.GetInstanceID();
                if (!seen.Add(id)) continue;
                if (!IsAnyChest(container)) continue;
                nearbyContainers.Add(container);
            }

            foreach (var container in nearbyContainers)
            {
                var inventory = container.GetInventory();
                if (inventory == null) continue;

                ContainerHelper.EnsureLoaded(container, inventory);

                var existingItems = new HashSet<string>();
                foreach (var item in inventory.GetAllItems())
                    if (item != null) existingItems.Add(item.m_shared.m_name);
                if (existingItems.Count == 0) continue;

                int dropCount = Physics.OverlapSphereNonAlloc(container.transform.position, radius, _overlapBuffer);
                for (int j = 0; j < dropCount; j++)
                {
                    var drop = _overlapBuffer[j].GetComponentInParent<ItemDrop>();
                    if (drop == null || drop.m_itemData == null) continue;
                    if (!drop.m_autoPickup) continue;
                    if (!existingItems.Contains(drop.m_itemData.m_shared.m_name)) continue;

                    if (inventory.CanAddItem(drop.m_itemData, drop.m_itemData.m_stack))
                    {
                        inventory.AddItem(drop.m_itemData);
                        ChestVFX.Play(container.gameObject);

                        var nview = drop.GetComponent<ZNetView>();
                        if (nview != null && nview.IsValid())
                        {
                            nview.ClaimOwnership();
                            nview.Destroy();
                        }
                        else
                        {
                            UnityEngine.Object.Destroy(drop.gameObject);
                        }
                    }
                }
            }
        }

        private static bool IsAnyChest(Container container)
        {
            string name = container.gameObject.name.ToLower();
            if (name.Contains("private")) return false;
            if (name.Contains("chest") || name.Contains("barrel")) return true;
            return false;
        }
    }

    // ==================== QUICK DEPOSIT ====================

    public static class QuickDepositHelper
    {
        private static readonly Collider[] _overlapBuffer = new Collider[128];

        public static void DepositMatchingItems(Player player, float radius)
        {
            var playerInventory = player.GetInventory();
            if (playerInventory == null) return;

            var nearbyContainers = new List<Container>();
            var seen = new HashSet<int>();
            int hitCount = Physics.OverlapSphereNonAlloc(player.transform.position, radius, _overlapBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var container = _overlapBuffer[i].GetComponentInParent<Container>();
                if (container == null) continue;
                int id = container.GetInstanceID();
                if (!seen.Add(id)) continue;
                if (!IsAnyChest(container)) continue;
                nearbyContainers.Add(container);
            }

            if (nearbyContainers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No chests nearby");
                return;
            }

            int totalDeposited = 0;
            var affectedChests = new HashSet<Container>();

            foreach (var container in nearbyContainers)
            {
                var chestInventory = container.GetInventory();
                if (chestInventory == null) continue;

                ContainerHelper.EnsureLoaded(container, chestInventory);

                var chestItemNames = new HashSet<string>();
                foreach (var chestItem in chestInventory.GetAllItems())
                    if (chestItem != null) chestItemNames.Add(chestItem.m_shared.m_name);
                if (chestItemNames.Count == 0) continue;

                var toDeposit = new List<ItemDrop.ItemData>();
                foreach (var playerItem in playerInventory.GetAllItems())
                {
                    if (playerItem == null) continue;
                    if (playerItem.m_equipped) continue;
                    if (!chestItemNames.Contains(playerItem.m_shared.m_name)) continue;
                    toDeposit.Add(playerItem);
                }

                foreach (var item in toDeposit)
                {
                    int stack = item.m_stack;
                    if (chestInventory.CanAddItem(item, stack))
                    {
                        chestInventory.AddItem(item);
                        playerInventory.RemoveItem(item);
                        totalDeposited += stack;
                        affectedChests.Add(container);
                    }
                }
            }

            foreach (var chest in affectedChests)
                ChestVFX.Play(chest.gameObject);

            if (totalDeposited > 0)
                player.Message(MessageHud.MessageType.Center, $"Deposited {totalDeposited} items into {affectedChests.Count} chest(s)");
            else
                player.Message(MessageHud.MessageType.Center, "No matching items to deposit");
        }

        private static bool IsAnyChest(Container container)
        {
            string name = container.gameObject.name.ToLower();
            if (name.Contains("private")) return false;
            if (name.Contains("chest") || name.Contains("barrel")) return true;
            return false;
        }
    }

    // ==================== CONTAINER HELPER ====================

    public static class ContainerHelper
    {
        private static readonly MethodInfo _loadMethod;
        private static bool _loggedOnce = false;

        static ContainerHelper()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            _loadMethod = typeof(Container).GetMethod("Load", flags)
                       ?? typeof(Container).GetMethod("LoadInventory", flags)
                       ?? typeof(Container).GetMethod("ReadInventory", flags);
        }

        public static void EnsureLoaded(Container container, Inventory inventory)
        {
            if (inventory.GetAllItems().Count > 0) return;

            var nview = container.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return;
            var zdo = nview.GetZDO();
            if (zdo == null) return;

            if (_loadMethod != null)
            {
                try
                {
                    _loadMethod.Invoke(container, null);
                    if (inventory.GetAllItems().Count > 0)
                    {
                        if (!_loggedOnce)
                        {
                            MegaQoLPlugin.Log($"[ContainerHelper] Loaded via reflection ({_loadMethod.Name})");
                            _loggedOnce = true;
                        }
                        return;
                    }
                }
                catch { }
            }

            try
            {
                string data = zdo.GetString(ZDOVars.s_items, "");
                if (!string.IsNullOrEmpty(data))
                {
                    ZPackage pkg = new ZPackage(data);
                    inventory.Load(pkg);
                    if (!_loggedOnce)
                    {
                        MegaQoLPlugin.Log($"[ContainerHelper] Loaded via ZDO direct read ({inventory.GetAllItems().Count} items)");
                        _loggedOnce = true;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                if (!_loggedOnce)
                {
                    MegaQoLPlugin.LogWarning($"[ContainerHelper] ZDO direct read failed: {ex.Message}");
                    _loggedOnce = true;
                }
            }

            try
            {
                var getStringMethod = typeof(ZDO).GetMethod("GetString", new[] { typeof(int), typeof(string) });
                if (getStringMethod != null)
                {
                    int itemsHash = "items".GetStableHashCode();
                    string data = (string)getStringMethod.Invoke(zdo, new object[] { itemsHash, "" });
                    if (!string.IsNullOrEmpty(data))
                    {
                        ZPackage pkg = new ZPackage(data);
                        inventory.Load(pkg);
                        if (!_loggedOnce)
                        {
                            MegaQoLPlugin.Log($"[ContainerHelper] Loaded via hash lookup ({inventory.GetAllItems().Count} items)");
                            _loggedOnce = true;
                        }
                        return;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Find all containers within radius of a position using physics overlap.
        /// </summary>
        public static List<Container> FindNearbyContainers(Vector3 position, float radius)
        {
            var result = new List<Container>();
            var seen = new HashSet<int>();
            var buffer = new Collider[128];
            int hitCount = Physics.OverlapSphereNonAlloc(position, radius, buffer);
            for (int i = 0; i < hitCount; i++)
            {
                var container = buffer[i].GetComponentInParent<Container>();
                if (container == null) continue;
                int id = container.GetInstanceID();
                if (!seen.Add(id)) continue;
                string name = container.gameObject.name.ToLower();
                if (name.Contains("private")) continue;
                if (name.Contains("chest") || name.Contains("barrel"))
                    result.Add(container);
            }
            return result;
        }
    }

    // ==================== CHEST VFX ====================

    public static class ChestVFX
    {
        private const string PowerEffectName = "fx_GP_Activation";

        public static void Play(GameObject target)
        {
            if (target == null) return;
            if (ZNetScene.instance == null) return;

            var prefab = ZNetScene.instance.GetPrefab(PowerEffectName);
            if (prefab == null) return;

            var fx = UnityEngine.Object.Instantiate(prefab, target.transform.position + Vector3.up * 1f, Quaternion.identity);
            if (fx != null)
                UnityEngine.Object.Destroy(fx, 5f);
        }
    }

    // ==================== CRAFT FROM CONTAINERS ====================

    /// <summary>
    /// Patches the crafting system so that when a player crafts or upgrades an item,
    /// materials are pulled from nearby containers if the player's inventory doesn't
    /// have enough. Also makes the "have resources" check aware of container contents
    /// so recipes show as craftable.
    /// </summary>
    [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece.Requirement[]), typeof(bool), typeof(int), typeof(HashSet<string>) })]
    public static class Player_HaveRequirements_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Piece.Requirement[] resources, bool discover, int qualityLevel, ref bool __result)
        {
            if (__result) return; // Already have enough
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return;
            if (__instance != Player.m_localPlayer) return;
            if (discover) return; // Discovery check, don't count containers

            var playerInventory = __instance.GetInventory();
            if (playerInventory == null) return;

            var nearbyContainers = ContainerHelper.FindNearbyContainers(
                __instance.transform.position, MegaQoLPlugin.CraftFromContainersRadius.Value);
            if (nearbyContainers.Count == 0) return;

            // Ensure all container inventories are loaded
            foreach (var c in nearbyContainers)
                ContainerHelper.EnsureLoaded(c, c.GetInventory());

            // Check if ALL requirements are met with player + container inventories combined
            bool allMet = true;
            foreach (var req in resources)
            {
                if (req.m_resItem == null) continue;
                int needed = req.GetAmount(qualityLevel);
                if (needed <= 0) continue;

                string itemName = req.m_resItem.m_itemData.m_shared.m_name;
                int have = playerInventory.CountItems(itemName);

                foreach (var container in nearbyContainers)
                {
                    var inv = container.GetInventory();
                    if (inv != null) have += inv.CountItems(itemName);
                }

                if (have < needed) { allMet = false; break; }
            }
            __result = allMet;
        }
    }

    // Also patch the Recipe-based overload used for crafting station recipes
    [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Recipe), typeof(bool), typeof(int) })]
    public static class Player_HaveRequirements_Recipe_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Recipe recipe, bool discover, int qualityLevel, ref bool __result)
        {
            if (__result) return;
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return;
            if (__instance != Player.m_localPlayer) return;
            if (discover) return;

            var playerInventory = __instance.GetInventory();
            if (playerInventory == null) return;
            if (recipe.m_resources == null) return;

            var nearbyContainers = ContainerHelper.FindNearbyContainers(
                __instance.transform.position, MegaQoLPlugin.CraftFromContainersRadius.Value);
            if (nearbyContainers.Count == 0) return;

            foreach (var c in nearbyContainers)
                ContainerHelper.EnsureLoaded(c, c.GetInventory());

            bool allMet = true;
            foreach (var req in recipe.m_resources)
            {
                if (req.m_resItem == null) continue;
                int needed = req.GetAmount(qualityLevel);
                if (needed <= 0) continue;

                string itemName = req.m_resItem.m_itemData.m_shared.m_name;
                int have = playerInventory.CountItems(itemName);

                foreach (var container in nearbyContainers)
                {
                    var inv = container.GetInventory();
                    if (inv != null) have += inv.CountItems(itemName);
                }

                if (have < needed) { allMet = false; break; }
            }
            __result = allMet;
        }
    }

    /// <summary>
    /// When consuming resources for crafting, pull from nearby containers for items
    /// the player doesn't have enough of in their own inventory.
    /// </summary>
    [HarmonyPatch(typeof(Player), "ConsumeResources")]
    public static class Player_ConsumeResources_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality)
        {
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return true;
            if (__instance != Player.m_localPlayer) return true;

            var playerInventory = __instance.GetInventory();
            if (playerInventory == null) return true;

            var nearbyContainers = ContainerHelper.FindNearbyContainers(
                __instance.transform.position, MegaQoLPlugin.CraftFromContainersRadius.Value);

            // If no nearby containers, let vanilla handle it
            if (nearbyContainers.Count == 0) return true;

            foreach (var c in nearbyContainers)
                ContainerHelper.EnsureLoaded(c, c.GetInventory());

            foreach (var req in requirements)
            {
                if (req.m_resItem == null) continue;
                int needed = req.GetAmount(qualityLevel);
                if (needed <= 0) continue;

                string itemName = req.m_resItem.m_itemData.m_shared.m_name;

                // First consume from player inventory
                int playerHas = playerInventory.CountItems(itemName);
                int fromPlayer = Mathf.Min(playerHas, needed);
                if (fromPlayer > 0)
                {
                    playerInventory.RemoveItem(itemName, fromPlayer);
                    needed -= fromPlayer;
                }

                // Then consume remainder from nearby containers
                if (needed > 0)
                {
                    foreach (var container in nearbyContainers)
                    {
                        if (needed <= 0) break;
                        var inv = container.GetInventory();
                        if (inv == null) continue;

                        int containerHas = inv.CountItems(itemName);
                        int fromContainer = Mathf.Min(containerHas, needed);
                        if (fromContainer > 0)
                        {
                            inv.RemoveItem(itemName, fromContainer);
                            needed -= fromContainer;
                            ChestVFX.Play(container.gameObject);
                        }
                    }
                }
            }

            // We handled all consumption, skip the original method
            return false;
        }
    }

    // ==================== MAP TELEPORT ====================

    [HarmonyPatch(typeof(Minimap), "OnMapMiddleClick")]
    public static class Minimap_OnMapMiddleClick_Teleport_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Minimap __instance)
        {
            if (!MegaQoLPlugin.EnableMapTeleport.Value) return;

            Player player = Player.m_localPlayer;
            if (player == null) return;

            try
            {
                var screenToWorldMethod = typeof(Minimap).GetMethod("ScreenToWorldPoint",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (screenToWorldMethod == null) return;

                Vector3 mousePos = Input.mousePosition;
                Vector3 worldPos = (Vector3)screenToWorldMethod.Invoke(__instance, new object[] { mousePos });

                float groundHeight = ZoneSystem.instance.GetGroundHeight(worldPos);
                if (groundHeight > worldPos.y)
                    worldPos.y = groundHeight + 1f;
                else
                    worldPos.y += 1f;

                __instance.SetMapMode(Minimap.MapMode.None);
                player.TeleportTo(worldPos, player.transform.rotation, true);
                player.Message(MessageHud.MessageType.Center, "Teleported!");
            }
            catch { }
        }
    }

    // ==================== BALLISTA IMPROVEMENTS ====================

    [HarmonyPatch(typeof(Turret), "Awake")]
    public static class Turret_Awake_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;

            __instance.m_targetPlayers = false;
            __instance.m_targetTamed = false;
            __instance.m_targetEnemies = true;
        }
    }

    public static class BallistaFriendlyHelper
    {
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
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            var haveTargetField = typeof(Turret).GetField("m_haveTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField != null) targetField.SetValue(turret, null);
            if (haveTargetField != null) haveTargetField.SetValue(turret, false);
        }
    }

    [HarmonyPatch(typeof(Turret), "FixedUpdate")]
    public static class Turret_FixedUpdate_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField == null) return;
            var target = targetField.GetValue(__instance) as Character;
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField == null) return;
            var target = targetField.GetValue(__instance) as Character;
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
        }
    }

    [HarmonyPatch(typeof(Turret), "UpdateTarget")]
    public static class Turret_UpdateTarget_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            __instance.m_targetPlayers = false;
            __instance.m_targetTamed = false;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField != null)
            {
                var currentTarget = targetField.GetValue(__instance) as Character;
                if (currentTarget != null && BallistaFriendlyHelper.IsFriendlyToPlayer(currentTarget))
                    BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField == null) return;
            var target = targetField.GetValue(__instance) as Character;
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
        }
    }

    [HarmonyPatch(typeof(Turret), "UpdateAttack")]
    public static class Turret_UpdateAttack_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return true;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField == null) return true;
            var target = targetField.GetValue(__instance) as Character;
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                var haveTargetField = typeof(Turret).GetField("m_haveTarget", BindingFlags.NonPublic | BindingFlags.Instance);
                if (haveTargetField != null)
                {
                    targetField.SetValue(__instance, null);
                    haveTargetField.SetValue(__instance, false);
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Turret), "ShootProjectile")]
    public static class Turret_ShootProjectile_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return true;
            var targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
            var haveTargetField = typeof(Turret).GetField("m_haveTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField == null || haveTargetField == null) return true;
            var target = targetField.GetValue(__instance) as Character;
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                targetField.SetValue(__instance, null);
                haveTargetField.SetValue(__instance, false);
                return false;
            }
            return true;
        }
    }

    // ==================== PLANT ANYWHERE ====================

    public static class PlantAnywhereHelper
    {
        private static readonly string[] TreeKeywords = new string[]
        {
            "beech", "birch", "oak", "fir", "pine", "tree", "ygg", "ancient"
        };

        public static bool IsCropPlant(GameObject obj)
        {
            if (obj == null) return false;
            string name = obj.name.ToLower();
            foreach (var keyword in TreeKeywords)
                if (name.Contains(keyword)) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Plant), "Awake")]
    public static class Plant_Awake_PlantAnywhere_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Plant __instance)
        {
            if (!MegaQoLPlugin.EnablePlantAnywhere.Value) return;
            if (!PlantAnywhereHelper.IsCropPlant(__instance.gameObject)) return;
            __instance.m_biome = Heightmap.Biome.All;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class Player_UpdatePlacementGhost_PlantAnywhere_Patch
    {
        private static readonly FieldInfo _placementGhostField = typeof(Player).GetField("m_placementGhost", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            if (!MegaQoLPlugin.EnablePlantAnywhere.Value) return;
            if (_placementGhostField == null) return;
            var ghost = _placementGhostField.GetValue(__instance) as GameObject;
            if (ghost == null) return;
            var plant = ghost.GetComponent<Plant>();
            if (plant == null) return;
            if (!PlantAnywhereHelper.IsCropPlant(ghost)) return;
            var piece = ghost.GetComponent<Piece>();
            if (piece != null)
                piece.m_onlyInBiome = Heightmap.Biome.None;
        }
    }

    // ==================== REMOVE BUILD DUST EFFECT ====================

    [HarmonyPatch(typeof(Player), "PlacePiece")]
    [HarmonyPatch(new Type[] { typeof(Piece), typeof(Vector3), typeof(Quaternion), typeof(bool) })]
    public static class Player_PlacePiece_NoDustPatch
    {
        private static FieldInfo _placeEffectField = typeof(Piece).GetField("m_placeEffect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static EffectList _dust_originalEffect;
        private static bool _dust_applied;

        [HarmonyPrefix]
        public static void Prefix(Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
        {
            if (!MegaQoLPlugin.EnableNoBuildDust.Value) return;
            _dust_applied = false;
            if (_placeEffectField == null || piece == null) return;
            _dust_originalEffect = (EffectList)_placeEffectField.GetValue(piece);
            if (_dust_originalEffect == null || _dust_originalEffect.m_effectPrefabs == null) return;

            var soundOnly = new List<EffectList.EffectData>();
            foreach (var e in _dust_originalEffect.m_effectPrefabs)
            {
                if (e.m_prefab == null) continue;
                bool hasAudio = e.m_prefab.GetComponent<AudioSource>() != null || e.m_prefab.GetComponent<ZSFX>() != null;
                bool hasParticles = e.m_prefab.GetComponent<ParticleSystem>() != null || e.m_prefab.GetComponentInChildren<ParticleSystem>() != null;
                if (hasAudio && !hasParticles)
                    soundOnly.Add(e);
            }

            var filtered = new EffectList { m_effectPrefabs = soundOnly.ToArray() };
            _placeEffectField.SetValue(piece, filtered);
            _dust_applied = true;
        }

        [HarmonyPostfix]
        public static void Postfix(Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
        {
            if (!_dust_applied || _placeEffectField == null || piece == null) return;
            _placeEffectField.SetValue(piece, _dust_originalEffect);
            _dust_applied = false;
        }
    }

    // ==================== MESSAGEHUD SMART QUEUE PATCH ====================

    [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
    public static class MessageHud_ShowMessage_OverwritePickupPatch
    {
        private static FieldInfo _queueField = typeof(MessageHud).GetField("m_msgQeue", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo _timerField = typeof(MessageHud).GetField("m_msgQueueTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        public static void Prefix(MessageHud __instance, MessageHud.MessageType type)
        {
            if (type != MessageHud.MessageType.TopLeft) return;
            if (_queueField == null) return;

            var queue = _queueField.GetValue(__instance);
            if (queue != null)
            {
                var clearMethod = queue.GetType().GetMethod("Clear");
                if (clearMethod != null)
                    clearMethod.Invoke(queue, null);
            }

            if (_timerField != null)
                _timerField.SetValue(__instance, 999f);
        }
    }
}
