using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace NetworkFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class NetworkFix : BaseUnityPlugin
    {
        #region[Declarations]
        public const string PluginAuthor = "Dalayeth";
        public const string PluginGuid = "com.github.dalayeth.Networkfix";
        public const string PluginName = "Network Fix";
        public const string PluginVersion = "1.0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<int> MaxQueueSize;
        internal static ConfigEntry<int> MaxBandwidth;
        internal static ConfigEntry<int> MinBandwidth;
        #endregion


        private static readonly Harmony harmony = new Harmony(PluginGuid);

        private void Awake()
        {
            Log = Logger;
            MaxQueueSize = Config.Bind("Networking", "MaxQueueSize", 30720, "The max queue possible. Default: 10,240.");
            MinBandwidth = Config.Bind("Networking", "MinBandwidth", 153600, "The max bandwidth that Valheim will use. Default: 153,600 Max: 524,288");
            MaxBandwidth = Config.Bind("Networking", "MaxBandwidth", 460800, "The max bandwidth that Valheim will use. Default: 153,600 Max: 524,288");
            if (MaxBandwidth.Value > 524288) {MaxBandwidth.Value = 524288;}
            if (MinBandwidth.Value > MaxBandwidth.Value) {MinBandwidth.Value = MaxBandwidth.Value;}
            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}