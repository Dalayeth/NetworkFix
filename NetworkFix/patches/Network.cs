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
                    NetworkFix.Log.LogDebug(instruction);
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
                NetworkFix.isServerSet = true;
            }
        }

        /// [connection int32] Upper limit of buffered pending bytes to be sent,
        /// if this is reached SendMessage will return k_EResultLimitExceeded
        /// Default is 512k (524288 bytes)
        /// k_ESteamNetworkingConfig_SendBufferSize = 9,


        /// [connection int32] Minimum/maximum send rate clamp, 0 is no limit.
        /// This value will control the min/max allowed sending rate that 
        /// bandwidth estimation is allowed to reach.  Default is 0 (no-limit)
        /// k_ESteamNetworkingConfig_SendRateMin = 10,
        /// k_ESteamNetworkingConfig_SendRateMax = 11,


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
            GCHandle SendRateMin = GCHandle.Alloc(NetworkFix.SendRateMin.Value, GCHandleType.Pinned);
            GCHandle SendRateMax = GCHandle.Alloc(NetworkFix.SendRateMax.Value, GCHandleType.Pinned);
            SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMin.AddrOfPinnedObject());
            SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
            NetworkFix.Log.LogInfo($"SendRateMin has been changed to {NetworkFix.SendRateMin.Value}");
            NetworkFix.Log.LogInfo($"SendRateMax has been changed to {NetworkFix.SendRateMax.Value}");
            if (NetworkFix.SendRateMax.Value > 524288)
            {
                NetworkFix.Log.LogInfo($"SendRateMax has been set over the default Steam SendBufferSize.");
                SteamGameServerNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
                NetworkFix.Log.LogInfo($"Steam's SendBufferSize has been changed to {NetworkFix.SendRateMax.Value}");
            }
            SendRateMin.Free();
            SendRateMax.Free();
        }

        static void GamePatch()
        {
            GCHandle SendRateMin = GCHandle.Alloc(NetworkFix.SendRateMin.Value, GCHandleType.Pinned);
            GCHandle SendRateMax = GCHandle.Alloc(NetworkFix.SendRateMax.Value, GCHandleType.Pinned);
            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMin.AddrOfPinnedObject());
            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
            NetworkFix.Log.LogInfo($"SendRateMin has been changed to {NetworkFix.SendRateMin.Value}");
            NetworkFix.Log.LogInfo($"SendRateMax has been changed to {NetworkFix.SendRateMax.Value}");
            if (NetworkFix.SendRateMax.Value > 524288)
            {
                NetworkFix.Log.LogInfo($"SendRateMax has been set over the default Steam SendBufferSize.");
                SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, SendRateMax.AddrOfPinnedObject());
                NetworkFix.Log.LogInfo($"Steam's SendBufferSize has been changed to {NetworkFix.SendRateMax.Value}");
            }
            SendRateMin.Free();
            SendRateMax.Free();
        }

        [HarmonyPatch]
        static class RegisterGlobalCallbackPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZSteamSocket), "RegisterGlobalCallbacks")]
            static void RegisterGlobalCallbacks()
            {
                if (!NetworkFix.isServerSet) { return; }
                if (NetworkFix.isServer)
                {
                    ServerPatch();
                }
                else
                {
                    GamePatch();
                }
                //int test = 0;
                //ESteamNetworkingConfigDataType test3;
                //NativeMethods.ISteamNetworkingUtils_GetConfigValue(CSteamAPIContext.GetSteamNetworkingUtils(), ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin, ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero, out test3, (IntPtr)test, out ulong test2);
                //NetworkFix.Log.LogInfo($"test = {test}  :  test2 = {test2}");
            }
        }


        // [HarmonyPatch(typeof(ZSteamSocket))]
        // internal static class Patch_MinBandwidth
        // {
        // [HarmonyPatch(nameof(ZSteamSocket.RegisterGlobalCallbacks))]
        // [HarmonyTranspiler]
        // private static IEnumerable<CodeInstruction> ModifyRGC(IEnumerable<CodeInstruction> instructions)
        // {
        // /*
        // 0x0004807F 1F0A          IL_007B: ldc.i4.s  10
        // 0x00048081 17            IL_007D: ldc.i4.1
        // 0x00048082 7E0907000A    IL_007E: ldsfld    native int [mscorlib]System.IntPtr::Zero
        // 0x00048087 17            IL_0083: ldc.i4.1
        // 0x00048088 1202          IL_0084: ldloca.s  V_2
        // */
        // IEnumerable<CodeInstruction> newInstructions = new CodeMatcher(instructions)
        // .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)10))
        // .MatchForward(false, new CodeMatch(OpCodes.Ldloca_S))
        // .Set(OpCodes.Ldc_I4, NetworkFix.SendRateMin.Value)
        //Return new modified instructions
        // .InstructionEnumeration();

        // #if DEBUG
        // NetworkFix.Log.LogDebug($"{newInstructions}");
        // var codes = new List<CodeInstruction>(instructions);
        // var newCodes = new List<CodeInstruction>(newInstructions);
        // for (var i = 0; i < codes.Count; i++)
        // {
        //NetworkFix.Log.LogDebug($"{newCodes[i]}");
        // if (codes[i].opcode == OpCodes.Ldloca_S)
        // {
        // if (codes[i] != newCodes[i])
        // {
        // NetworkFix.Log.LogDebug($"MinBandwidth has been changed from {codes[i].opcode}: {codes[i].operand} to {newCodes[i].opcode}: {newCodes[i].operand}");
        // }
        // }
        // }
        // #endif
        // NetworkFix.Log.LogInfo($"SendRateMin has been changed to {NetworkFix.SendRateMin.Value}");
        // return instructions;
        // }
        // }

        // [HarmonyPatch(typeof(ZSteamSocket))]
        // internal static class Patch_MaxBandwidth
        // {
        // [HarmonyPatch(nameof(ZSteamSocket.RegisterGlobalCallbacks))]
        // [HarmonyTranspiler]
        // private static IEnumerable<CodeInstruction> ModifyRGC(IEnumerable<CodeInstruction> instructions)
        // {
        // /*
        // 0x0004807F 1F0A          IL_007B: ldc.i4.s  11
        // 0x00048081 17            IL_007D: ldc.i4.1
        // 0x00048082 7E0907000A    IL_007E: ldsfld    native int [mscorlib]System.IntPtr::Zero
        // 0x00048087 17            IL_0083: ldc.i4.1
        // 0x00048088 1202          IL_0084: ldloca.s  V_2
        // */
        // IEnumerable<CodeInstruction> newInstructions = new CodeMatcher(instructions)
        // .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)11))
        // .MatchForward(false, new CodeMatch(OpCodes.Ldloca_S))
        // .Set(OpCodes.Ldc_I4, NetworkFix.SendRateMax.Value)
        //Return new modified instructions
        // .InstructionEnumeration();

        // #if DEBUG
        // NetworkFix.Log.LogDebug($"{newInstructions}");
        // var codes = new List<CodeInstruction>(instructions);
        // var newCodes = new List<CodeInstruction>(newInstructions);
        // for (var i = 0; i < codes.Count; i++)
        // {
        //NetworkFix.Log.LogDebug($"{newCodes[i]}");
        // if (codes[i].opcode == OpCodes.Ldloca_S)
        // {
        // if (codes[i] != newCodes[i])
        // {
        // NetworkFix.Log.LogDebug($"MaxBandwidth has been changed from {codes[i].opcode}: {codes[i].operand} to {newCodes[i].opcode}: {newCodes[i].operand}");
        // }
        // }
        // }
        // #endif
        // NetworkFix.Log.LogInfo($"SendRateMax has been changed to {NetworkFix.SendRateMax.Value}");
        // return instructions;
        // }
        // }
    }
}
