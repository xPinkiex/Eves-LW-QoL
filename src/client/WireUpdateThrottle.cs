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
	// Buffers incoming circuit state updates so the client does less work
	// recoloring wires/pegs and updating components at high simulation speeds.
	//  - Frozen: nothing is applied until unfrozen, then everything is applied at once.
	//  - Interval > 0: buffered states are applied at most once per interval.
	// Only the newest state per circuit index is kept, so memory stays bounded.
	public static class WireUpdateThrottle
	{
		public static bool Frozen { get; private set; }

		// 0 = apply every frame (vanilla behaviour).
		public static int IntervalMs { get; private set; }

		private static readonly Dictionary<int, bool> Pending = new Dictionary<int, bool>();
		private static readonly Stopwatch SinceFlush = Stopwatch.StartNew();
		private static IClientWorld bufferedWorld;

		private static bool Buffering => Frozen || IntervalMs > 0;

		public static bool ToggleFreeze()
		{
			Frozen = !Frozen;
			LConsole.WriteLine(Frozen
				? "[Eves_QoL] Wire updates frozen (circuit keeps running)."
				: "[Eves_QoL] Wire updates resumed.");
			SendFreezeToServer();
			if (!Frozen)
			{
				Flush();
			}
			return true;
		}

		// If the server also runs Eves_QoL, ask it to stop sending state packets entirely.
		private static void SendFreezeToServer()
		{
			if (Instances.MainWorld == null || !PacketManager.TryGetCodeFromType(typeof(WireFreezeRequest), out ushort _))
			{
				return;
			}
			Instances.SendData.Send(new WireFreezeRequest { Frozen = Frozen });
		}

		[Command("EvesQoL.WireUpdateInterval", Description = "Apply wire/peg state updates at most once every N milliseconds. 0 = every frame (default).")]
		public static void SetInterval(int milliseconds)
		{
			IntervalMs = milliseconds < 0 ? 0 : milliseconds;
			LConsole.WriteLine($"[Eves_QoL] Wire update interval set to {IntervalMs} ms.");
			if (IntervalMs == 0 && !Frozen)
			{
				Flush();
			}
		}

		[Command("EvesQoL.FreezeWires", Description = "Toggle freezing of wire/peg/component state visuals.")]
		public static void FreezeCommand()
		{
			ToggleFreeze();
		}

		// Returns true if the packet was swallowed into the buffer.
		internal static bool TryBuffer(CircuitStatesUpdatePacket packet)
		{
			if (!Buffering)
			{
				return false;
			}

			var world = Instances.MainWorld;
			if (world != bufferedWorld)
			{
				// Joined a different world while buffering: old states are meaningless.
				Pending.Clear();
				bufferedWorld = world;
				// New connection: the server doesn't know we're frozen yet.
				if (Frozen)
				{
					SendFreezeToServer();
				}
			}

			var updates = packet.Updates;
			for (int i = 0; i < updates.Length; i++)
			{
				Pending[updates[i].Key] = updates[i].Value;
			}
			return true;
		}

		// Called once per frame, right before the game processes its color buffer.
		internal static void FrameTick()
		{
			if (Frozen || Pending.Count == 0)
			{
				return;
			}
			if (IntervalMs > 0 && SinceFlush.ElapsedMilliseconds < IntervalMs)
			{
				return;
			}
			Flush();
		}

		private static void Flush()
		{
			SinceFlush.Restart();
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

		public static void Prefix()
		{
			WireUpdateThrottle.FrameTick();
		}
	}
}
