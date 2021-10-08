using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace NetworkFix.Patches
{
    class Network
    {
        [HarmonyPatch(typeof(ZDOMan))]
        internal static class Patch_SendZDOs
        {
            [HarmonyPatch(nameof(ZDOMan.SendZDOs))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ModifyZDOs(IEnumerable<CodeInstruction> instructions)
            {
                IEnumerable<CodeInstruction> newInstructions = new CodeMatcher(instructions)
                    // Move to right before int gets added to stack
                    .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, 10240))
                    .Repeat(matcher => matcher
                        .Set(OpCodes.Ldc_I4, NetworkFix.MaxSendQueueSize.Value)
                    )
                    // Return new modified instructions
                    .InstructionEnumeration();
#if DEBUG
                foreach (CodeInstruction instruction in newInstructions)
                {
                    if (instruction == new CodeInstruction(OpCodes.Ldc_I4, NetworkFix.MaxSendQueueSize.Value))
                    {
                        NetworkFix.Log.LogDebug($"Instruction {instruction} successfully changed to {NetworkFix.MaxSendQueueSize.Value}");
                    }
                }
#endif
                NetworkFix.Log.LogInfo($"MaxSendQueueSize has been changed to {NetworkFix.MaxSendQueueSize.Value}");
                return newInstructions;
            }
        }

        [HarmonyPatch]
        static class AwakePatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZNet), "Awake")]
            static void Awake(ref bool ___m_isServer)
            {
                NetworkFix.isServer = ___m_isServer;
                NetworkFix.isZNetAwake = true;
#if DEBUG
                NetworkFix.Log.LogDebug($"IsServer set to: {NetworkFix.isServer}");
#endif
            }
        }

        [HarmonyPatch]
        static class OnDestroyPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ZNet), "OnDestroy")]
            static void OnDestroy()
            {
                NetworkFix.isServer = true;
                NetworkFix.isZNetAwake = false;
            }
        }

        [HarmonyPatch]
        static class RegisterGlobalCallbackPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZSteamSocket), "RegisterGlobalCallbacks")]
            static void RegisterGlobalCallbacks()
            {
                if (NetworkFix.hasFailed || !NetworkFix.isZNetAwake) { return; }
                if (NetworkFix.isServer)
                {
                    try
                    {
                        ServerPatch();
                    }
                    catch
                    {
                        NetworkFix.Log.LogWarning($"Failed to patch server bandwidth. Disabling bandwidth patch.");
                        NetworkFix.hasFailed = true;
                    }
                }
                else
                {
                    try
                    {
                        ClientPatch();
                    }
                    catch
                    {
                        NetworkFix.Log.LogWarning($"Failed to patch client bandwidth. Disabling bandwidth patch.");
                        NetworkFix.hasFailed = true;
                    }
                }
            }
        }


        /// [connection int32] Upper limit of buffered pending bytes to be sent,
        /// if this is reached SendMessage will return k_EResultLimitExceeded
        /// Default is 512k (524288 bytes)
        /// k_ESteamNetworkingConfig_SendBufferSize = 9,
        /// 
        /// [connection int32] Minimum/maximum send rate clamp, 0 is no limit.
        /// This value will control the min/max allowed sending rate that 
        /// bandwidth estimation is allowed to reach.  Default is 0 (no-limit)
        /// k_ESteamNetworkingConfig_SendRateMin = 10,
        /// k_ESteamNetworkingConfig_SendRateMax = 11,
        /// 
        /// [connection int32] Nagle time, in microseconds.  When SendMessage is called, if
        /// the outgoing message is less than the size of the MTU, it will be
        /// queued for a delay equal to the Nagle timer value.  This is to ensure
        /// that if the application sends several small messages rapidly, they are
        /// coalesced into a single packet.
        /// See historical RFC 896.  Value is in microseconds. 
        /// Default is 5000us (5ms).
        /// k_ESteamNetworkingConfig_NagleTime = 12,

        static void ServerPatch()
        {
            try
            {
                GCHandle SendRateMin = GCHandle.Alloc(NetworkFix.SendRateMin.Value, GCHandleType.Pinned);
                GCHandle SendRateMax = GCHandle.Alloc(NetworkFix.SendRateMax.Value, GCHandleType.Pinned);
                SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMin.AddrOfPinnedObject());
                NetworkFix.Log.LogInfo($"Server's SendRateMin has been changed to {NetworkFix.SendRateMin.Value}");
                SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
                NetworkFix.Log.LogInfo($"Server's SendRateMax has been changed to {NetworkFix.SendRateMax.Value}");
                if (NetworkFix.SendRateMax.Value > 524288)
                {
                    NetworkFix.Log.LogInfo($"Server's SendRateMax has been set over the default Steam SendBufferSize.");
                    SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
                    NetworkFix.Log.LogInfo($"Steam's SendBufferSize has been changed to {NetworkFix.SendRateMax.Value}");
                }
                SendRateMin.Free();
                SendRateMax.Free();
            }
            catch
            {
                NetworkFix.Log.LogInfo($"Local Server detected. Now patching for client.");
                try
                {
                    ClientPatch();
                }
                catch
                {
                    NetworkFix.Log.LogWarning($"Failed to patch client bandwidth. Disabling bandwidth patch.");
                    NetworkFix.hasFailed = true;
                }
            }
        }

        static void ClientPatch()
        {
            GCHandle SendRateMin = GCHandle.Alloc(NetworkFix.SendRateMin.Value, GCHandleType.Pinned);
            GCHandle SendRateMax = GCHandle.Alloc(NetworkFix.SendRateMax.Value, GCHandleType.Pinned);
            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMin.AddrOfPinnedObject());
            NetworkFix.Log.LogInfo($"Client's SendRateMin has been changed to {NetworkFix.SendRateMin.Value}");
            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
            NetworkFix.Log.LogInfo($"Client's SendRateMax has been changed to {NetworkFix.SendRateMax.Value}");
            if (NetworkFix.SendRateMax.Value > 524288)
            {
                NetworkFix.Log.LogInfo($"Client's SendRateMax has been set over the default Steam SendBufferSize.");
                SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
                NetworkFix.Log.LogInfo($"Steam's SendBufferSize has been changed to {NetworkFix.SendRateMax.Value}");
            }
            SendRateMin.Free();
            SendRateMax.Free();
        }
    }
}