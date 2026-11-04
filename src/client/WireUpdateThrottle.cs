using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Eves_QoL.Shared;
using LICC;
using LogicAPI.Networking.Packets.FromServer;
using LogicWorld.Interfaces;
using LogicWorld.Networking.Handlers;
using LogicWorld.SharedCode.Networking;

namespace Eves_QoL.Client
{
	// Two ways to cut client-side work at high simulation speeds:
	//
	//  - Wire freeze (F7): circuit states are still received and applied, so displays
	//    and other components keep updating. Only the per-frame wire/peg recolor step is
	//    skipped. The game's own recolor buffer keeps collecting changed indexes
	//    (deduplicated), so unfreezing recolors everything in one frame.
	//    WireUpdateInterval throttles that same step instead of skipping it.
	//
	//  - Full freeze (F5): nothing updates. If the server runs Eves_QoL it stops sending
	//    state packets entirely; otherwise packets are buffered here (newest state per
	//    index) and applied on unfreeze.
	public static class WireUpdateThrottle
	{
		public static bool WiresFrozen { get; private set; }

		public static bool FullyFrozen { get; private set; }

		// 0 = recolor wires every frame (vanilla behaviour).
		public static int IntervalMs { get; private set; }

		private static readonly Dictionary<int, bool> Pending = new Dictionary<int, bool>();
		private static readonly Stopwatch SinceRecolor = Stopwatch.StartNew();
		private static IClientWorld bufferedWorld;

		public static bool ToggleWireFreeze()
		{
			WiresFrozen = !WiresFrozen;
			LConsole.WriteLine(WiresFrozen
				? "[Eves_QoL] Wire visuals frozen (displays still update)."
				: "[Eves_QoL] Wire visuals resumed.");
			return true;
		}

		public static bool ToggleFullFreeze()
		{
			FullyFrozen = !FullyFrozen;
			LConsole.WriteLine(FullyFrozen
				? "[Eves_QoL] All circuit visuals frozen (circuit keeps running)."
				: "[Eves_QoL] All circuit visuals resumed.");
			SendFreezeToServer();
			if (!FullyFrozen)
			{
				Flush();
			}
			return true;
		}

		[Command("EvesQoL.FreezeWires", Description = "Toggle freezing wire/peg colors. Displays and other components keep updating.")]
		public static void FreezeWiresCommand()
		{
			ToggleWireFreeze();
		}

		[Command("EvesQoL.FreezeAll", Description = "Toggle freezing all circuit visuals, including displays. Stops state packets if the server has Eves_QoL.")]
		public static void FreezeAllCommand()
		{
			ToggleFullFreeze();
		}

		[Command("EvesQoL.WireUpdateInterval", Description = "Recolor wires/pegs at most once every N milliseconds. Displays are unaffected. 0 = every frame (default).")]
		public static void SetInterval(int milliseconds)
		{
			IntervalMs = milliseconds < 0 ? 0 : milliseconds;
			LConsole.WriteLine($"[Eves_QoL] Wire update interval set to {IntervalMs} ms.");
		}

		// If the server also runs Eves_QoL, ask it to stop/resume sending state packets.
		private static void SendFreezeToServer()
		{
			if (Instances.MainWorld == null || !PacketManager.TryGetCodeFromType(typeof(WireFreezeRequest), out ushort _))
			{
				return;
			}
			Instances.SendData.Send(new WireFreezeRequest { Frozen = FullyFrozen });
		}

		// Returns true if the packet was swallowed into the buffer.
		internal static bool TryBuffer(CircuitStatesUpdatePacket packet)
		{
			if (!FullyFrozen)
			{
				return false;
			}

			var world = Instances.MainWorld;
			if (world != bufferedWorld)
			{
				// Joined a different world while frozen: old states are meaningless.
				Pending.Clear();
				bufferedWorld = world;
				// New connection: the server doesn't know we're frozen yet.
				SendFreezeToServer();
			}

			var updates = packet.Updates;
			for (int i = 0; i < updates.Length; i++)
			{
				Pending[updates[i].Key] = updates[i].Value;
			}
			return true;
		}

		// Decides whether the game's per-frame wire recolor step runs this frame.
		internal static bool ShouldRecolorThisFrame()
		{
			if (WiresFrozen || FullyFrozen)
			{
				return false;
			}
			if (IntervalMs > 0)
			{
				if (SinceRecolor.ElapsedMilliseconds < IntervalMs)
				{
					return false;
				}
				SinceRecolor.Restart();
			}
			return true;
		}

		private static void Flush()
		{
			if (Pending.Count == 0)
			{
				return;
			}

			var world = Instances.MainWorld;
			if (world != null && world == bufferedWorld)
			{
				var states = world.CircuitStates;
				foreach (var kv in Pending)
				{
					states.SetStateAt(kv.Key, kv.Value);
				}
			}
			Pending.Clear();
		}
	}

	[HarmonyPatch(typeof(CircuitStatesUpdateHandler), nameof(CircuitStatesUpdateHandler.Handle))]
	public static class CircuitStatesUpdateHandlerPatch
	{
		public static bool Prefix(CircuitStatesUpdatePacket packet)
		{
			return !WireUpdateThrottle.TryBuffer(packet);
		}
	}

	// CircuitStatesManager is internal, so target it by name.
	// Skipping this leaves the game's recolor buffer intact; it is processed on a later frame.
	[HarmonyPatch]
	public static class ColorBufferFramePatch
	{
		public static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("LogicWorld.Rendering.CircuitStatesManager"),
				"ProcessCircuitStateChangeColorBuffer"
			);
		}

		public static bool Prefix()
		{
			return WireUpdateThrottle.ShouldRecolorThisFrame();
		}
	}
}
