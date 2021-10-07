using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace NetworkFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class NetworkFix : BaseUnityPlugin
    {
        #region[Declarations]
        public const string PluginAuthor = "Dalayeth";
        public const string PluginGuid = "com.github.dalayeth.Networkfix";
        public const string PluginName = "Network Fix";
        public const string PluginVersion = "1.1.0";

        internal static ManualLogSource Log;
        public static bool isServer { get; set; } = true;
        public static bool isZNetAwake { get; set; } = false;
        public static bool hasFailed { get; set; } = false;

        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<int> MaxSendQueueSize;
        internal static ConfigEntry<int> SendRateMin;
        internal static ConfigEntry<int> SendRateMax;
        private Harmony _harmony;
        #endregion

        private void Awake()
        {
            #region[Config]
            Log = Logger;
            ModEnabled = Config.Bind("Default", "Enabled", true);
            MaxSendQueueSize = Config.Bind("Networking", "MaxSendQueueSize", 30720, "The max send queue possible. Game default: 10,240.");
            SendRateMin = Config.Bind("Networking", "SendRateMin", 524288, "Minimum send rate clamp. This value will control the min allowed sending rate that bandwidth estimation is allowed to reach. Game default: 153,600");
            SendRateMax = Config.Bind("Networking", "SendRateMax", 524288, "Maximum send rate clamp. This value will control the max allowed sending rate that bandwidth estimation is allowed to reach. Game default: 153,600");
            if (SendRateMax.Value < 0) { SendRateMax.Value = 0; }
            if (SendRateMin.Value < 0) { SendRateMin.Value = 0; }
            if (SendRateMax.Value != 0)
            {
                if (SendRateMin.Value > SendRateMax.Value) { SendRateMin.Value = SendRateMax.Value; }
            }
            #endregion

            if (ModEnabled.Value)
            {
                _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginGuid);
            }
        }

        private void OnDestroy()
        {
            if (ModEnabled.Value)
            {
                _harmony.UnpatchSelf();
            }
        }
    }
}