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
        public const string PluginVersion = "1.6.1";

        private static ManualLogSource _logger;
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
        public static ConfigEntry<bool> EnableQuickDeposit;
        public static ConfigEntry<KeyCode> QuickDepositKey;
        public static ConfigEntry<float> QuickDepositRadius;

        // Craft from Containers
        public static ConfigEntry<bool> EnableCraftFromContainers;
        public static ConfigEntry<float> CraftFromContainersRadius;

        // Map Teleport
        public static ConfigEntry<bool> EnableMapTeleport;

        // Plant Anywhere
        public static ConfigEntry<bool> EnablePlantAnywhere;

        // Build Dust Removal
        public static ConfigEntry<bool> EnableNoBuildDust;

        // Rune Build (bypass no-build zones)
        public static ConfigEntry<bool> EnableRuneBuild;

        // MessageHud Smart Queue
        public static ConfigEntry<bool> EnableMessageHudQueue;

        // Mass Farming
        public static ConfigEntry<bool> EnableMassFarming;
        public static ConfigEntry<KeyCode> MassFarmingKey;
        public static ConfigEntry<float> MassHarvestRadius;
        public static ConfigEntry<int> PlantGridWidth;
        public static ConfigEntry<int> PlantGridLength;
        public static ConfigEntry<bool> GridIgnoreStamina;
        public static ConfigEntry<bool> GridIgnoreDurability;

        // Debug
        public static ConfigEntry<bool> DebugMode;

        // Timers
        private static float _autoRefuelTimer = 0f;
        private static float _autoPetFeederTimer = 0f;
        private static float _ballistaAutoReloadTimer = 0f;
        private static float _autoRepairTimer = 0f;
        private static float _chestAutoPickupTimer = 0f;

        private void Awake()
        {
            _logger = Logger;
            _logger.LogInfo($"{PluginName} v{PluginVersion} is loading...");

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

            // 4. Pet Feeder
            EnableAutoPetFeeder = Config.Bind("4. Pet Feeder", "Enable", true,
                "Automatically feeds tamed creatures from nearby containers");
            AutoPetFeederRange = Config.Bind("4. Pet Feeder", "Range", 10f,
                new ConfigDescription("Range to search for containers in meters", new AcceptableValueRange<float>(0f, 1000f)));
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
                new ConfigDescription("Player item auto-pickup radius in meters (vanilla = 2)", new AcceptableValueRange<float>(1f, 1000f)));
            EnableChestAutoPickup = Config.Bind("5. Item Management", "EnableChestAutoPickup", false,
                "Chests automatically pull in nearby ground items that match their contents");
            ChestAutoPickupRadius = Config.Bind("5. Item Management", "ChestAutoPickupRadius", 10f,
                new ConfigDescription("Radius around chests to pull in matching ground items", new AcceptableValueRange<float>(1f, 1000f)));
            ChestAutoPickupInterval = Config.Bind("5. Item Management", "ChestAutoPickupInterval", 2f,
                new ConfigDescription("How often chests check for nearby items (seconds)", new AcceptableValueRange<float>(0.5f, 30f)));
            EnableQuickDeposit = Config.Bind("5. Item Management", "EnableQuickDeposit", true,
                "Enables quick deposit hotkey to deposit matching items into nearby chests");
            QuickDepositKey = Config.Bind("5. Item Management", "QuickDepositKey", KeyCode.Period,
                "Hotkey to deposit matching items from inventory into nearby chests");
            QuickDepositRadius = Config.Bind("5. Item Management", "QuickDepositRadius", 10f,
                new ConfigDescription("Radius to search for chests when quick-depositing", new AcceptableValueRange<float>(1f, 1000f)));

            // 6. Craft from Containers
            EnableCraftFromContainers = Config.Bind("6. Craft from Containers", "Enable", true,
                "Allows crafting stations to pull materials from nearby containers");
            CraftFromContainersRadius = Config.Bind("6. Craft from Containers", "Radius", 10f,
                new ConfigDescription("Radius to search for containers when crafting", new AcceptableValueRange<float>(1f, 1000f)));

            // 7. Map Teleport
            EnableMapTeleport = Config.Bind("7. Map Teleport", "Enable", true,
                "Enables map teleportation - middle-click on map to teleport to that location");

            // 8. Plant Anywhere
            EnablePlantAnywhere = Config.Bind("8. Plant Anywhere", "Enable", true,
                "Enables planting crops in any biome (removes biome restrictions for non-tree plantables)");

            // 9. Build Dust Removal
            EnableNoBuildDust = Config.Bind("9. Build Dust Removal", "Enable", true,
                "Removes dust/particle effects when placing build pieces (keeps sound effects)");

            // 10. Rune Build
            EnableRuneBuild = Config.Bind("10. Rune Build", "Enable", true,
                "Bypass the 'mystical force' no-build restriction near starting runestones and other no-build locations");

            // 11. MessageHud Smart Queue
            EnableMessageHudQueue = Config.Bind("11. MessageHud Smart Queue", "Enable", true,
                "Enables smart message queue - clears stale messages so the latest one shows immediately");

            // 12. Mass Farming
            EnableMassFarming = Config.Bind("12. Mass Farming", "Enable", true,
                "Hold hotkey while interacting to mass-harvest pickables, or while planting to grid-plant");
            MassFarmingKey = Config.Bind("12. Mass Farming", "Hotkey", KeyCode.LeftShift,
                "Hold this key to activate mass farming features");
            MassHarvestRadius = Config.Bind("12. Mass Farming", "HarvestRadius", 5f,
                new ConfigDescription("Radius for mass harvesting pickables", new AcceptableValueRange<float>(1f, 1000f)));
            PlantGridWidth = Config.Bind("12. Mass Farming", "PlantGridWidth", 5,
                new ConfigDescription("Width of grid when mass planting (odd numbers recommended)", new AcceptableValueRange<int>(1, 15)));
            PlantGridLength = Config.Bind("12. Mass Farming", "PlantGridLength", 5,
                new ConfigDescription("Length of grid when mass planting (odd numbers recommended)", new AcceptableValueRange<int>(1, 15)));
            GridIgnoreStamina = Config.Bind("12. Mass Farming", "IgnoreStamina", false,
                "Ignore stamina cost when grid planting extra plants");
            GridIgnoreDurability = Config.Bind("12. Mass Farming", "IgnoreDurability", false,
                "Ignore cultivator durability when grid planting");

            // 13. Debug
            DebugMode = Config.Bind("13. Debug", "DebugMode", false,
                "Enable verbose debug logging to BepInEx console/log");

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

        public static void Log(string message) { if (DebugMode.Value) _logger?.LogInfo(message); }
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
            if (EnableQuickDeposit.Value && Input.GetKeyDown(QuickDepositKey.Value))
            {
                if (IsUIBlockingInput())
                {
                    Log("[QuickDeposit] Key pressed but UI is blocking input");
                }
                else
                {
                    QuickDepositHelper.DepositMatchingItems(player, QuickDepositRadius.Value);
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
        // Cache reflection lookups
        private static readonly FieldInfo _consumeItemsField = typeof(MonsterAI).GetField("m_consumeItems", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo _onConsumedMethod = typeof(Tameable).GetMethod("OnConsumedItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static void FeedNearbyPets(Vector3 position, float range)
        {
            float rangeSq = range * range;
            var allCharacters = Character.GetAllCharacters();

            foreach (var character in allCharacters)
            {
                if (character == null) continue;
                if (!character.IsTamed()) continue;
                if (character is Player) continue;
                if ((position - character.transform.position).sqrMagnitude > rangeSq) continue;

                var tameable = character.GetComponent<Tameable>();
                if (tameable == null) continue;

                TryFeedCreature(tameable, position, rangeSq);
            }
        }

        private static void TryFeedCreature(Tameable tameable, Vector3 position, float rangeSq)
        {
            var monsterAI = tameable.GetComponent<MonsterAI>();
            if (monsterAI == null) return;
            if (_consumeItemsField == null) return;

            var consumeItems = _consumeItemsField.GetValue(monsterAI) as List<ItemDrop>;
            if (consumeItems == null || consumeItems.Count == 0) return;

            var validFoodNames = new HashSet<string>();
            foreach (var foodItem in consumeItems)
                if (foodItem != null) validFoodNames.Add(foodItem.gameObject.name);

            if (!tameable.IsHungry()) return;

            foreach (var container in ContainerHelper.AllContainers)
            {
                if (container == null) continue;
                if ((position - container.transform.position).sqrMagnitude > rangeSq) continue;
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
                        if (_onConsumedMethod != null)
                            _onConsumedMethod.Invoke(tameable, new object[] { null });
                        return;
                    }
                }
            }
        }

        private static bool IsAllowedContainer(Container container)
        {
            var type = ContainerHelper.GetContainerType(container);
            switch (type)
            {
                case ContainerType.BlackMetalChest: return MegaQoLPlugin.AutoPetFeederUseBlackMetalChests.Value;
                case ContainerType.Barrel: return MegaQoLPlugin.AutoPetFeederUseBarrels.Value;
                case ContainerType.ReinforcedChest: return MegaQoLPlugin.AutoPetFeederUseReinforcedChests.Value;
                case ContainerType.WoodChest: return MegaQoLPlugin.AutoPetFeederUseChests.Value;
                default: return false;
            }
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
                validAmmoNames.AddRange(new[] { "TurretBolt", "TurretBoltWood", "TurretBoltFlametal" });
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
        private const int MAX_CONTAINERS_PER_PICKUP = 32;

        public static void PickupNearbyItems(Vector3 playerPosition, float radius)
        {
            var nearbyContainers = ContainerHelper.FindNearbyContainers(playerPosition, radius);
            int count = Mathf.Min(nearbyContainers.Count, MAX_CONTAINERS_PER_PICKUP);

            for (int c = 0; c < count; c++)
            {
                var container = nearbyContainers[c];
                if (container == null) continue;

                var containerNview = container.GetComponent<ZNetView>();
                if (containerNview == null || !containerNview.IsValid()) continue;

                var inventory = container.GetInventory();
                if (inventory == null) continue;

                ContainerHelper.EnsureLoaded(container, inventory);

                var existingItems = new HashSet<string>();
                foreach (var item in inventory.GetAllItems())
                    if (item != null) existingItems.Add(item.m_shared.m_name);
                if (existingItems.Count == 0) continue;

                bool containerModified = false;
                // Use a tighter radius (capped at 5m per container) for the physics query
                float pickupRange = Mathf.Min(radius, 5f);
                int dropCount = Physics.OverlapSphereNonAlloc(container.transform.position, pickupRange, _overlapBuffer);
                for (int j = 0; j < dropCount; j++)
                {
                    var drop = _overlapBuffer[j].GetComponentInParent<ItemDrop>();
                    if (drop == null || drop.m_itemData == null) continue;
                    if (!drop.m_autoPickup) continue;
                    if (!existingItems.Contains(drop.m_itemData.m_shared.m_name)) continue;

                    if (inventory.CanAddItem(drop.m_itemData, drop.m_itemData.m_stack))
                    {
                        if (!containerModified)
                        {
                            containerNview.ClaimOwnership();
                            containerModified = true;
                        }

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

                if (containerModified)
                    QuickDepositHelper.SaveContainerToZDO(container);
            }
        }
    }

    // ==================== QUICK DEPOSIT ====================

    public static class QuickDepositHelper
    {
        public static void DepositMatchingItems(Player player, float radius)
        {
            var playerInventory = player.GetInventory();
            if (playerInventory == null) return;

            var nearbyContainers = ContainerHelper.FindNearbyContainers(player.transform.position, radius);

            if (nearbyContainers.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No chests nearby");
                return;
            }

            // Build eligible items ONCE — skip hotbar (row 0) and equipped
            var eligible = new List<ItemDrop.ItemData>();
            var eligibleNames = new HashSet<string>();
            foreach (var playerItem in playerInventory.GetAllItems())
            {
                if (playerItem == null) continue;
                if (playerItem.m_equipped) continue;
                if (playerItem.m_gridPos.y == 0) continue;
                eligible.Add(playerItem);
                eligibleNames.Add(playerItem.m_shared.m_name);
            }

            if (eligible.Count == 0)
            {
                player.Message(MessageHud.MessageType.Center, "No depositable items");
                return;
            }

            int totalDeposited = 0;
            var affectedChests = new HashSet<Container>();

            foreach (var container in nearbyContainers)
            {
                if (eligible.Count == 0) break;    // Everything deposited — done

                var chestInventory = container.GetInventory();
                if (chestInventory == null) continue;

                ContainerHelper.EnsureLoaded(container, chestInventory);

                // Quick check: does this chest contain any item names we have?
                var chestItemNames = new HashSet<string>();
                foreach (var chestItem in chestInventory.GetAllItems())
                {
                    if (chestItem == null) continue;
                    if (eligibleNames.Contains(chestItem.m_shared.m_name))
                        chestItemNames.Add(chestItem.m_shared.m_name);
                }

                if (chestItemNames.Count == 0) continue;

                for (int i = eligible.Count - 1; i >= 0; i--)
                {
                    var item = eligible[i];
                    if (!chestItemNames.Contains(item.m_shared.m_name)) continue;

                    int stack = item.m_stack;
                    if (chestInventory.CanAddItem(item, stack))
                    {
                        chestInventory.AddItem(item);
                        playerInventory.RemoveItem(item);
                        totalDeposited += stack;
                        affectedChests.Add(container);
                        eligible.RemoveAt(i);
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

        /// <summary>
        /// Writes a container's in-memory inventory to its ZDO so changes persist.
        /// Used by ChestAutoPickup.
        /// </summary>
        public static void SaveContainerToZDO(Container container)
        {
            try
            {
                var nview = container.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;

                var inv = container.GetInventory();
                if (inv == null) return;

                ZPackage pkg = new ZPackage();
                inv.Save(pkg);
                nview.GetZDO().Set(ZDOVars.s_items, pkg.GetBase64());
            }
            catch (Exception ex)
            {
                MegaQoLPlugin.LogError($"[SaveContainerToZDO] Error: {ex.Message}");
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

    // ==================== CONTAINER HELPER ====================

    public static class ContainerHelper
    {
        public static readonly HashSet<Container> AllContainers = new HashSet<Container>();

        // Type cache — classified once at registration, no repeated string ops
        private static readonly Dictionary<Container, ContainerType> _typeCache = new Dictionary<Container, ContainerType>();

        // Nearby container cache — avoids iterating all containers every call
        private static readonly List<Container> _nearbyCache = new List<Container>();
        private static Vector3 _nearbyCachePos;
        private static float _nearbyCacheRadius;
        private static float _nearbyCacheTime;
        private const float NEARBY_CACHE_TTL = 1.0f;

        // Combined material count cache for CraftFromContainers
        private static readonly Dictionary<string, int> _materialCache = new Dictionary<string, int>();
        private static float _materialCacheTime;
        private static Vector3 _materialCachePos;
        private static float _materialCacheRadius;
        private const float MATERIAL_CACHE_TTL = 0.5f;

        // Stale entry pruning
        private static float _lastPruneTime;
        private const float PRUNE_INTERVAL = 30f;

        // Reflection for loading
        private static readonly MethodInfo _loadMethod;
        private static readonly MethodInfo _getStringHashMethod;
        private static readonly int _itemsHash;

        static ContainerHelper()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            _loadMethod = typeof(Container).GetMethod("Load", flags)
                       ?? typeof(Container).GetMethod("LoadInventory", flags)
                       ?? typeof(Container).GetMethod("ReadInventory", flags);

            _getStringHashMethod = typeof(ZDO).GetMethod("GetString", new[] { typeof(int), typeof(string) });
            _itemsHash = "items".GetStableHashCode();
        }

        public static ContainerType ClassifyContainer(Container container)
        {
            string name = container.gameObject.name.ToLower();
            if (name.Contains("private"))
                return ContainerType.Private;
            if (name.Contains("piece_chest_blackmetal") || name.StartsWith("blackmetalchest"))
                return ContainerType.BlackMetalChest;
            if (name.Contains("incinerator") || name.Contains("obliterator"))
                return ContainerType.Obliterator;
            if (name.Contains("barrel"))
                return ContainerType.Barrel;
            if ((name.Contains("piece_chest") || name.StartsWith("reinforcedchest")) &&
                !name.Contains("wood") && !name.Contains("blackmetal"))
                return ContainerType.ReinforcedChest;
            if (name.Contains("chest"))
                return ContainerType.WoodChest;
            return ContainerType.Unknown;
        }

        public static void Register(Container container)
        {
            AllContainers.Add(container);
            _typeCache[container] = ClassifyContainer(container);
        }

        public static void Unregister(Container container)
        {
            AllContainers.Remove(container);
            _typeCache.Remove(container);
            InvalidateNearbyCache();
        }

        public static ContainerType GetContainerType(Container container)
        {
            if (_typeCache.TryGetValue(container, out var type))
                return type;
            return ContainerType.Unknown;
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
                    if (inventory.GetAllItems().Count > 0) return;
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
                    return;
                }
            }
            catch { }

            if (_getStringHashMethod != null)
            {
                try
                {
                    string data = (string)_getStringHashMethod.Invoke(zdo, new object[] { _itemsHash, "" });
                    if (!string.IsNullOrEmpty(data))
                    {
                        ZPackage pkg = new ZPackage(data);
                        inventory.Load(pkg);
                    }
                }
                catch { }
            }
        }

        public static void InvalidateNearbyCache()
        {
            _nearbyCacheTime = 0f;
            _materialCacheTime = 0f;
        }

        public static void InvalidateMaterialCache()
        {
            _materialCacheTime = 0f;
        }

        public static List<Container> FindNearbyContainers(Vector3 position, float radius)
        {
            float now = Time.time;

            // Prune stale entries periodically (destroyed containers from zone unloads)
            if (now - _lastPruneTime > PRUNE_INTERVAL)
            {
                _lastPruneTime = now;
                AllContainers.RemoveWhere(c => c == null);
                var staleKeys = new List<Container>();
                foreach (var kvp in _typeCache)
                    if (kvp.Key == null) staleKeys.Add(kvp.Key);
                foreach (var k in staleKeys) _typeCache.Remove(k);
            }

            // Return cached result if still valid
            if (now - _nearbyCacheTime < NEARBY_CACHE_TTL &&
                Mathf.Approximately(_nearbyCacheRadius, radius) &&
                (position - _nearbyCachePos).sqrMagnitude < 4f)
            {
                return _nearbyCache;
            }

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

        // Cached combined material counts (player inventory + nearby containers)
        public static int GetCombinedItemCount(string itemName, Player player, float radius)
        {
            EnsureMaterialCache(player, radius);
            return _materialCache.TryGetValue(itemName, out int count) ? count : 0;
        }

        private static void EnsureMaterialCache(Player player, float radius)
        {
            float now = Time.time;
            Vector3 pos = player.transform.position;

            if (now - _materialCacheTime < MATERIAL_CACHE_TTL &&
                Mathf.Approximately(_materialCacheRadius, radius) &&
                (pos - _materialCachePos).sqrMagnitude < 4f)
            {
                return;
            }

            _materialCache.Clear();
            _materialCachePos = pos;
            _materialCacheRadius = radius;
            _materialCacheTime = now;

            // Add player inventory
            var playerInv = player.GetInventory();
            if (playerInv != null)
            {
                foreach (var item in playerInv.GetAllItems())
                {
                    if (item == null) continue;
                    string name = item.m_shared.m_name;
                    if (_materialCache.ContainsKey(name))
                        _materialCache[name] += item.m_stack;
                    else
                        _materialCache[name] = item.m_stack;
                }
            }

            // Add nearby containers
            var containers = FindNearbyContainers(pos, radius);
            foreach (var container in containers)
            {
                var inv = container.GetInventory();
                if (inv == null) continue;
                EnsureLoaded(container, inv);
                foreach (var item in inv.GetAllItems())
                {
                    if (item == null) continue;
                    string name = item.m_shared.m_name;
                    if (_materialCache.ContainsKey(name))
                        _materialCache[name] += item.m_stack;
                    else
                        _materialCache[name] = item.m_stack;
                }
            }
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
    [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    public static class Player_HaveRequirements_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Recipe recipe, bool discover, int qualityLevel, ref bool __result)
        {
            if (__result) return;
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return;
            if (__instance != Player.m_localPlayer) return;
            if (discover) return;
            if (recipe.m_resources == null) return;

            float radius = MegaQoLPlugin.CraftFromContainersRadius.Value;

            bool allMet = true;
            foreach (var req in recipe.m_resources)
            {
                if (req.m_resItem == null) continue;
                int needed = req.GetAmount(qualityLevel);
                if (needed <= 0) continue;

                string itemName = req.m_resItem.m_itemData.m_shared.m_name;
                int have = ContainerHelper.GetCombinedItemCount(itemName, __instance, radius);
                if (have < needed) { allMet = false; break; }
            }
            __result = allMet;
        }
    }

    // Also patch the Piece-based overload used for building
    [HarmonyPatch(typeof(Player), "HaveRequirements", new Type[] { typeof(Piece), typeof(Player.RequirementMode) })]
    public static class Player_HaveRequirements_Piece_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, Piece piece, ref bool __result)
        {
            if (__result) return;
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return;
            if (__instance != Player.m_localPlayer) return;
            if (__instance.InPlaceMode()) return;
            if (piece.m_resources == null) return;

            float radius = MegaQoLPlugin.CraftFromContainersRadius.Value;

            bool allMet = true;
            foreach (var req in piece.m_resources)
            {
                if (req.m_resItem == null) continue;
                int needed = req.GetAmount(0);
                if (needed <= 0) continue;

                string itemName = req.m_resItem.m_itemData.m_shared.m_name;
                int have = ContainerHelper.GetCombinedItemCount(itemName, __instance, radius);
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
        public static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier)
        {
            if (!MegaQoLPlugin.EnableCraftFromContainers.Value) return true;
            if (__instance != Player.m_localPlayer) return true;
            if (__instance.InPlaceMode()) return true; // Planting/building uses inventory only

            var playerInventory = __instance.GetInventory();
            if (playerInventory == null) return true;

            var nearbyContainers = ContainerHelper.FindNearbyContainers(
                __instance.transform.position, MegaQoLPlugin.CraftFromContainersRadius.Value);

            // If no nearby containers, let vanilla handle it
            if (nearbyContainers.Count == 0) return true;

            foreach (var c in nearbyContainers)
            {
                if (c == null) continue;
                var inv = c.GetInventory();
                if (inv != null) ContainerHelper.EnsureLoaded(c, inv);
            }

            var affectedContainers = new HashSet<Container>();

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
                        if (container == null) continue;
                        var inv = container.GetInventory();
                        if (inv == null) continue;

                        int containerHas = inv.CountItems(itemName);
                        int fromContainer = Mathf.Min(containerHas, needed);
                        if (fromContainer > 0)
                        {
                            inv.RemoveItem(itemName, fromContainer);
                            needed -= fromContainer;
                            affectedContainers.Add(container);
                        }
                    }
                }
            }

            // Play VFX once per affected container
            foreach (var container in affectedContainers)
                ChestVFX.Play(container.gameObject);

            // Invalidate material cache since inventory contents changed
            ContainerHelper.InvalidateMaterialCache();

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
        // Cached reflection — looked up ONCE, not every frame
        private static readonly FieldInfo _targetField = typeof(Turret).GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _haveTargetField = typeof(Turret).GetField("m_haveTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _allowedAmmoField = typeof(Turret).GetField("m_allowedAmmo", BindingFlags.Public | BindingFlags.Instance);

        public static FieldInfo TargetField => _targetField;
        public static FieldInfo HaveTargetField => _haveTargetField;
        public static FieldInfo AllowedAmmoField => _allowedAmmoField;

        public static Character GetTarget(Turret turret)
        {
            if (_targetField == null) return null;
            return _targetField.GetValue(turret) as Character;
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
        }
    }

    [HarmonyPatch(typeof(Turret), "FixedUpdate")]
    public static class Turret_FixedUpdate_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
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
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Turret __instance)
        {
            if (!MegaQoLPlugin.EnableBallistaImprovements.Value) return;
            var target = BallistaFriendlyHelper.GetTarget(__instance);
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
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
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
            var target = BallistaFriendlyHelper.GetTarget(__instance);
            if (target != null && BallistaFriendlyHelper.IsFriendlyToPlayer(target))
            {
                BallistaFriendlyHelper.ClearFriendlyTarget(__instance);
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

    // ==================== RUNE BUILD (BYPASS NO-BUILD ZONES) ====================

    [HarmonyPatch(typeof(Location), "IsInsideNoBuildLocation")]
    public static class Location_IsInsideNoBuildLocation_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            if (!__result) return;
            if (!MegaQoLPlugin.EnableRuneBuild.Value) return;
            __result = false;
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
            if (!MegaQoLPlugin.EnableMessageHudQueue.Value) return;
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

    // ==================== MASS HARVEST ====================

    [HarmonyPatch(typeof(Player), "Interact")]
    public static class Player_Interact_MassHarvest_Patch
    {
        private static readonly FieldInfo _interactMaskField = AccessTools.Field(typeof(Player), "m_interactMask");
        private static readonly MethodInfo _extractMethod = AccessTools.Method(typeof(Beehive), "Extract");

        [HarmonyPrefix]
        public static void Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            if (__instance != Player.m_localPlayer) return;
            if (hold || __instance.InAttack() || __instance.InDodge()) return;
            if (!Input.GetKey(MegaQoLPlugin.MassFarmingKey.Value)) return;

            int interactMask = (int)_interactMaskField.GetValue(__instance);

            var pickable = go.GetComponentInParent<Pickable>();
            if (pickable != null)
            {
                var colliders = Physics.OverlapSphere(go.transform.position,
                    MegaQoLPlugin.MassHarvestRadius.Value, interactMask);
                foreach (var col in colliders)
                {
                    if (col == null) continue;
                    var other = col.gameObject.GetComponentInParent<Pickable>();
                    if (other != null && other != pickable &&
                        other.m_itemPrefab.name == pickable.m_itemPrefab.name)
                    {
                        other.Interact(__instance, false, alt);
                    }
                }
                return;
            }

            var beehive = go.GetComponentInParent<Beehive>();
            if (beehive != null && _extractMethod != null)
            {
                var colliders = Physics.OverlapSphere(go.transform.position,
                    MegaQoLPlugin.MassHarvestRadius.Value, interactMask);
                foreach (var col in colliders)
                {
                    if (col == null) continue;
                    var other = col.gameObject.GetComponentInParent<Beehive>();
                    if (other != null && other != beehive &&
                        PrivateArea.CheckAccess(other.transform.position, 0f, true, false))
                    {
                        _extractMethod.Invoke(other, null);
                    }
                }
            }
        }
    }

    // ==================== MASS PLANT (GRID PLANTING) ====================

    public static class MassPlantHelper
    {
        public static readonly FieldInfo PlacementGhostField = AccessTools.Field(typeof(Player), "m_placementGhost");
        public static readonly FieldInfo BuildPiecesField = AccessTools.Field(typeof(Player), "m_buildPieces");
        public static readonly FieldInfo NoPlacementCostField = AccessTools.Field(typeof(Player), "m_noPlacementCost");
        public static readonly MethodInfo GetRightItemMethod = AccessTools.Method(typeof(Humanoid), "GetRightItem");
        private static readonly int PlantSpaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");

        public static GameObject[] Ghosts = new GameObject[1];
        public static Piece FakeResourcePiece;

        public static bool PlaceSuccessful;
        public static Vector3 PlacedPosition;
        public static Quaternion PlacedRotation;
        public static Piece PlacedPiece;
        public static int? SavedRotation;

        public static bool IsHotkeyPressed => Input.GetKey(MegaQoLPlugin.MassFarmingKey.Value);

        public static List<Vector3> BuildGridPositions(Vector3 origin, Plant plant, Quaternion rotation)
        {
            float spacing = plant.m_growRadius * 2f;
            int width = MegaQoLPlugin.PlantGridWidth.Value;
            int length = MegaQoLPlugin.PlantGridLength.Value;

            var positions = new List<Vector3>(width * length);
            Vector3 left = rotation * Vector3.left * spacing;
            Vector3 forward = rotation * Vector3.forward * spacing;
            Vector3 start = origin - forward * (length / 2) - left * (width / 2);

            for (int i = 0; i < length; i++)
            {
                Vector3 pos = start;
                for (int j = 0; j < width; j++)
                {
                    pos.y = ZoneSystem.instance.GetGroundHeight(pos);
                    positions.Add(pos);
                    pos += left;
                }
                start += forward;
            }
            return positions;
        }

        public static bool HasGrowSpace(Vector3 pos, GameObject go)
        {
            var plant = go.GetComponent<Plant>();
            if (plant != null)
                return Physics.OverlapSphere(pos, plant.m_growRadius, PlantSpaceMask).Length == 0;
            return true;
        }

        public static void DestroyGhosts()
        {
            for (int i = 0; i < Ghosts.Length; i++)
            {
                if (Ghosts[i] != null)
                {
                    UnityEngine.Object.Destroy(Ghosts[i]);
                    Ghosts[i] = null;
                }
            }
            FakeResourcePiece = null;
        }

        public static void SetGhostsActive(bool active)
        {
            foreach (var g in Ghosts)
                if (g != null) g.SetActive(active);
        }

        public static bool EnsureGhosts(Player player)
        {
            int count = MegaQoLPlugin.PlantGridWidth.Value * MegaQoLPlugin.PlantGridLength.Value;
            if (Ghosts[0] == null || Ghosts.Length != count)
            {
                DestroyGhosts();
                if (Ghosts.Length != count)
                    Ghosts = new GameObject[count];

                var buildPieces = BuildPiecesField.GetValue(player) as PieceTable;
                if (buildPieces == null) return false;

                var prefab = buildPieces.GetSelectedPrefab();
                if (prefab == null) return false;
                if (prefab.GetComponent<Piece>().m_repairPiece) return false;

                for (int i = 0; i < Ghosts.Length; i++)
                    Ghosts[i] = CreateGhost(prefab);
            }

            if (FakeResourcePiece == null)
            {
                FakeResourcePiece = Ghosts[0].GetComponent<Piece>();
                FakeResourcePiece.m_dlc = string.Empty;
                FakeResourcePiece.m_resources = new Piece.Requirement[1] { new Piece.Requirement() };
            }
            return true;
        }

        public static GameObject CreateGhost(GameObject prefab)
        {
            ZNetView.m_forceDisableInit = true;
            var ghost = UnityEngine.Object.Instantiate(prefab);
            ZNetView.m_forceDisableInit = false;
            ghost.name = prefab.name;

            foreach (var joint in ghost.GetComponentsInChildren<Joint>())
                UnityEngine.Object.Destroy(joint);
            foreach (var rb in ghost.GetComponentsInChildren<Rigidbody>())
                UnityEngine.Object.Destroy(rb);
            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                UnityEngine.Object.Destroy(col);

            int layer = LayerMask.NameToLayer("ghost");
            foreach (var t in ghost.GetComponentsInChildren<Transform>())
                t.gameObject.layer = layer;

            foreach (var tm in ghost.GetComponentsInChildren<TerrainModifier>())
                UnityEngine.Object.Destroy(tm);
            foreach (var gp in ghost.GetComponentsInChildren<GuidePoint>())
                UnityEngine.Object.Destroy(gp);
            foreach (var light in ghost.GetComponentsInChildren<Light>())
                UnityEngine.Object.Destroy(light);

            var ghostOnly = ghost.transform.Find("_GhostOnly");
            if (ghostOnly != null)
                ghostOnly.gameObject.SetActive(true);

            foreach (var renderer in ghost.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.sharedMaterial == null) continue;
                var mats = renderer.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = new Material(mats[m]);
                    mats[m].SetFloat("_RippleDistance", 0f);
                    mats[m].SetFloat("_ValueNoise", 0f);
                }
                renderer.sharedMaterials = mats;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return ghost;
        }

        /// <summary>
        /// Finds the HaveRequirements(Piece, RequirementMode) overload via reflection
        /// to avoid enum type name issues across Valheim versions.
        /// </summary>
        private static MethodInfo _haveReqPieceMethod;
        private static bool _haveReqSearched;

        public static bool PlayerHaveRequirements(Player player, Piece piece)
        {
            if (!_haveReqSearched)
            {
                _haveReqSearched = true;
                foreach (var m in typeof(Player).GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name != "HaveRequirements") continue;
                    var ps = m.GetParameters();
                    if (ps.Length == 2 && ps[0].ParameterType == typeof(Piece))
                    {
                        _haveReqPieceMethod = m;
                        break;
                    }
                }
            }
            if (_haveReqPieceMethod != null)
            {
                var reqModeType = _haveReqPieceMethod.GetParameters()[1].ParameterType;
                var zeroVal = Enum.ToObject(reqModeType, 0); // 0 = CanBuild
                return (bool)_haveReqPieceMethod.Invoke(player, new object[] { piece, zeroVal });
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "TryPlacePiece")]
    public static class MassPlant_TryPlacePiece_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            if (MassPlantHelper.IsHotkeyPressed && !MassPlantHelper.SavedRotation.HasValue)
                MassPlantHelper.SavedRotation = ___m_placeRotation;
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref bool __result, Piece piece, ref int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            MassPlantHelper.PlaceSuccessful = __result;
            if (__result)
            {
                var ghost = MassPlantHelper.PlacementGhostField.GetValue(__instance) as GameObject;
                if (ghost != null)
                {
                    MassPlantHelper.PlacedPosition = ghost.transform.position;
                    MassPlantHelper.PlacedRotation = ghost.transform.rotation;
                }
                MassPlantHelper.PlacedPiece = piece;
            }
            if (MassPlantHelper.IsHotkeyPressed && MassPlantHelper.SavedRotation.HasValue)
                ___m_placeRotation = MassPlantHelper.SavedRotation.Value;
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacement")]
    public static class MassPlant_UpdatePlacement_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            MassPlantHelper.PlaceSuccessful = false;
            if (MassPlantHelper.IsHotkeyPressed && MassPlantHelper.SavedRotation.HasValue)
                ___m_placeRotation = MassPlantHelper.SavedRotation.Value;
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            if (MassPlantHelper.IsHotkeyPressed)
                MassPlantHelper.SavedRotation = ___m_placeRotation;

            if (!MassPlantHelper.PlaceSuccessful) return;

            var plant = MassPlantHelper.PlacedPiece?.gameObject.GetComponent<Plant>();
            if (plant == null || !MassPlantHelper.IsHotkeyPressed) return;

            var heightmap = Heightmap.FindHeightmap(MassPlantHelper.PlacedPosition);
            if (heightmap == null) return;

            var positions = MassPlantHelper.BuildGridPositions(
                MassPlantHelper.PlacedPosition, plant, MassPlantHelper.PlacedRotation);

            foreach (var pos in positions)
            {
                if (pos == MassPlantHelper.PlacedPosition) continue;
                if (MassPlantHelper.PlacedPiece.m_cultivatedGroundOnly && !heightmap.IsCultivated(pos)) continue;

                var rightItem = MassPlantHelper.GetRightItemMethod.Invoke(__instance, null) as ItemDrop.ItemData;
                if (rightItem == null) continue;

                if (!MegaQoLPlugin.GridIgnoreStamina.Value && !__instance.HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
                {
                    Hud.instance.StaminaBarUppgradeFlash();
                    break;
                }

                bool noCost = (bool)MassPlantHelper.NoPlacementCostField.GetValue(__instance);
                if (!noCost && !MassPlantHelper.PlayerHaveRequirements(__instance, MassPlantHelper.PlacedPiece))
                    break;

                if (!MassPlantHelper.HasGrowSpace(pos, MassPlantHelper.PlacedPiece.gameObject)) continue;

                var obj = UnityEngine.Object.Instantiate(MassPlantHelper.PlacedPiece.gameObject, pos, MassPlantHelper.PlacedRotation);
                var piece = obj.GetComponent<Piece>();
                if (piece != null) piece.SetCreator(__instance.GetPlayerID());

                MassPlantHelper.PlacedPiece.m_placeEffect.Create(pos, MassPlantHelper.PlacedRotation, obj.transform, 1f, -1);
                Game.instance.IncrementPlayerStat((PlayerStatType)2, 1f);
                __instance.ConsumeResources(MassPlantHelper.PlacedPiece.m_resources, 0, -1);

                if (!MegaQoLPlugin.GridIgnoreStamina.Value)
                    __instance.UseStamina(rightItem.m_shared.m_attack.m_attackStamina);

                if (!MegaQoLPlugin.GridIgnoreDurability.Value && rightItem.m_shared.m_useDurability)
                {
                    rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
                    if (rightItem.m_durability <= 0f) break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
    public static class MassPlant_SetupPlacementGhost_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            if (MassPlantHelper.IsHotkeyPressed && !MassPlantHelper.SavedRotation.HasValue)
                MassPlantHelper.SavedRotation = ___m_placeRotation;
        }

        [HarmonyPostfix]
        public static void Postfix(ref int ___m_placeRotation)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;
            if (MassPlantHelper.IsHotkeyPressed && MassPlantHelper.SavedRotation.HasValue)
                ___m_placeRotation = MassPlantHelper.SavedRotation.Value;
            MassPlantHelper.DestroyGhosts();
        }
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class MassPlant_UpdatePlacementGhost_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (!MegaQoLPlugin.EnableMassFarming.Value) return;

            var mainGhost = MassPlantHelper.PlacementGhostField.GetValue(__instance) as GameObject;
            if (mainGhost == null || !mainGhost.activeSelf || !MassPlantHelper.IsHotkeyPressed)
            {
                MassPlantHelper.SetGhostsActive(false);
                return;
            }

            var plant = mainGhost.GetComponent<Plant>();
            if (plant == null)
            {
                MassPlantHelper.SetGhostsActive(false);
                return;
            }

            if (!MassPlantHelper.EnsureGhosts(__instance))
            {
                MassPlantHelper.SetGhostsActive(false);
                return;
            }

            // Find the primary resource requirement
            Piece.Requirement primaryReq = null;
            foreach (var r in mainGhost.GetComponent<Piece>().m_resources)
            {
                if (r.m_resItem != null && r.m_amount > 0) { primaryReq = r; break; }
            }
            if (primaryReq == null) return;

            MassPlantHelper.FakeResourcePiece.m_resources[0].m_resItem = primaryReq.m_resItem;
            MassPlantHelper.FakeResourcePiece.m_resources[0].m_amount = primaryReq.m_amount;

            float stamina = __instance.GetStamina();
            var rightItem = MassPlantHelper.GetRightItemMethod.Invoke(__instance, null) as ItemDrop.ItemData;
            if (rightItem == null) return;

            var heightmap = Heightmap.FindHeightmap(mainGhost.transform.position);
            var positions = MassPlantHelper.BuildGridPositions(mainGhost.transform.position, plant, mainGhost.transform.rotation);

            for (int i = 0; i < MassPlantHelper.Ghosts.Length && i < positions.Count; i++)
            {
                Vector3 pos = positions[i];

                if (mainGhost.transform.position == pos)
                {
                    MassPlantHelper.Ghosts[i].SetActive(false);
                    continue;
                }

                MassPlantHelper.FakeResourcePiece.m_resources[0].m_amount += primaryReq.m_amount;
                MassPlantHelper.Ghosts[i].transform.position = pos;
                MassPlantHelper.Ghosts[i].transform.rotation = mainGhost.transform.rotation;
                MassPlantHelper.Ghosts[i].SetActive(true);

                bool invalid = false;
                if (mainGhost.GetComponent<Piece>().m_cultivatedGroundOnly && heightmap != null && !heightmap.IsCultivated(pos))
                    invalid = true;
                else if (!MassPlantHelper.HasGrowSpace(pos, mainGhost))
                    invalid = true;
                else if (!MegaQoLPlugin.GridIgnoreStamina.Value && stamina < rightItem.m_shared.m_attack.m_attackStamina)
                    invalid = true;
                else
                {
                    bool noCost = (bool)MassPlantHelper.NoPlacementCostField.GetValue(__instance);
                    if (!noCost && !MassPlantHelper.PlayerHaveRequirements(__instance, MassPlantHelper.FakeResourcePiece))
                        invalid = true;
                }

                stamina -= rightItem.m_shared.m_attack.m_attackStamina;
                MassPlantHelper.Ghosts[i].GetComponent<Piece>().SetInvalidPlacementHeightlight(invalid);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // VANILLA BUG FIX — MonsterAI.PheromoneFleeCheck NRE guard
    // ═══════════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(MonsterAI), "PheromoneFleeCheck")]
    public static class PheromoneFleeCheckNullGuard
    {
        static bool Prefix(MonsterAI __instance, Character target)
        {
            return __instance != null && target != null;
        }

        static Exception Finalizer(Exception __exception)
        {
            // Only swallow NRE from stale/destroyed references inside vanilla code
            if (__exception is NullReferenceException)
                return null;
            return __exception;
        }
    }
}
