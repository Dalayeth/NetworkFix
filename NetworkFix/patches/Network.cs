using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

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
                        .Set(OpCodes.Ldc_I4, NetworkFix.MaxQueueSize.Value)
                    )
                    // Return new modified instructions
                    .InstructionEnumeration();
#if DEBUG
                var codes = new List<CodeInstruction>(instructions);
                var newCodes = new List<CodeInstruction>(newInstructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4)
                    {
                        if (codes[i].operand != newCodes[i].operand)
                        {
                            NetworkFix.Log.LogDebug($"MaxQueueSize has been changed from {codes[i].operand} to {newCodes[i].operand}");
                        }
                    }
                }
#endif
                NetworkFix.Log.LogInfo($"MaxQueueSize has been changed to {NetworkFix.MaxQueueSize.Value}");
                return newInstructions;
            }
        }

        [HarmonyPatch(typeof(ZSteamSocket))]
        internal static class Patch_MinBandwidth
        {
            [HarmonyPatch(nameof(ZSteamSocket.RegisterGlobalCallbacks))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ModifyRGC(IEnumerable<CodeInstruction> instructions)
            {
                /*
	            0x0004807F 1F0A          IL_007B: ldc.i4.s  10
	            0x00048081 17            IL_007D: ldc.i4.1
	            0x00048082 7E0907000A    IL_007E: ldsfld    native int [mscorlib]System.IntPtr::Zero
	            0x00048087 17            IL_0083: ldc.i4.1
	            0x00048088 1202          IL_0084: ldloca.s  V_2
                */
                IEnumerable<CodeInstruction> newInstructions = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)10))
                    .MatchForward(false, new CodeMatch(OpCodes.Ldloca_S))
                    .Set(OpCodes.Ldc_I4, NetworkFix.MinBandwidth.Value)
                    // Return new modified instructions
                    .InstructionEnumeration();

#if DEBUG
                NetworkFix.Log.LogDebug($"{newInstructions}");
                var codes = new List<CodeInstruction>(instructions);
                var newCodes = new List<CodeInstruction>(newInstructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    //NetworkFix.Log.LogDebug($"{newCodes[i]}");
                    if (codes[i].opcode == OpCodes.Ldloca_S)
                    {
                        if (codes[i] != newCodes[i])
                        {
                         NetworkFix.Log.LogDebug($"MinBandwidth has been changed from {codes[i].opcode}: {codes[i].operand} to {newCodes[i].opcode}: {newCodes[i].operand}");
                        }
                    }
                }
#endif
                NetworkFix.Log.LogInfo($"MaxQueueSize has been changed to {NetworkFix.MinBandwidth.Value}");
                return instructions;
            }
        }

        [HarmonyPatch(typeof(ZSteamSocket))]
        internal static class Patch_MaxBandwidth
        {
            [HarmonyPatch(nameof(ZSteamSocket.RegisterGlobalCallbacks))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ModifyRGC(IEnumerable<CodeInstruction> instructions)
            {
                /*
	            0x0004807F 1F0A          IL_007B: ldc.i4.s  11
	            0x00048081 17            IL_007D: ldc.i4.1
	            0x00048082 7E0907000A    IL_007E: ldsfld    native int [mscorlib]System.IntPtr::Zero
	            0x00048087 17            IL_0083: ldc.i4.1
	            0x00048088 1202          IL_0084: ldloca.s  V_2
                */
                IEnumerable<CodeInstruction> newInstructions = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)11))
                    .MatchForward(false, new CodeMatch(OpCodes.Ldloca_S))
                    .Set(OpCodes.Ldc_I4, NetworkFix.MaxBandwidth.Value)
                    // Return new modified instructions
                    .InstructionEnumeration();

#if DEBUG
                NetworkFix.Log.LogDebug($"{newInstructions}");
                var codes = new List<CodeInstruction>(instructions);
                var newCodes = new List<CodeInstruction>(newInstructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    //NetworkFix.Log.LogDebug($"{newCodes[i]}");
                    if (codes[i].opcode == OpCodes.Ldloca_S)
                    {
                        if (codes[i] != newCodes[i])
                        {
                            NetworkFix.Log.LogDebug($"MinBandwidth has been changed from {codes[i].opcode}: {codes[i].operand} to {newCodes[i].opcode}: {newCodes[i].operand}");
                        }
                    }
                }
#endif
                NetworkFix.Log.LogInfo($"MaxQueueSize has been changed to {NetworkFix.MaxBandwidth.Value}");
                return instructions;
            }
        }
    }
}