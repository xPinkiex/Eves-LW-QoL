using System.Collections.Generic;
using EccsLogicWorldAPI.Server;
using EccsLogicWorldAPI.Server.Injectors;
using Eves_QoL.Shared;
using HarmonyLib;
using LogicAPI.Networking;
using LogicAPI.Server;
using LogicAPI.Server.Networking;
using LogicWorld.Server.Networking;
using LogicWorld.SharedCode.Networking;

namespace Eves_QoL.Server
{
	public class EvesQoLServer : ServerMod
	{
		internal static NetworkServer Network;

		// Connections that asked not to receive circuit state updates.
		internal static readonly HashSet<Connection> FrozenClients = new HashSet<Connection>();

		protected override void Initialize()
		{
			Network = ServiceGetter.getService<NetworkServer>();
			Network.RemoteDisconnected += connection =>
			{
				lock (FrozenClients)
				{
					FrozenClients.Remove(connection);
				}
			};

			RawPacketHandlerInjector.addPacketHandler(new WireFreezeRequestHandler());
			new Harmony("Eves_QoL.Server").PatchAll();
		}
	}

	public class WireFreezeRequestHandler : PacketHandler<WireFreezeRequest>
	{
		public override void Handle(WireFreezeRequest packet, HandlerContext context)
		{
			lock (EvesQoLServer.FrozenClients)
			{
				if (packet.Frozen)
				{
					EvesQoLServer.FrozenClients.Add(context.Sender);
				}
				else
				{
					EvesQoLServer.FrozenClients.Remove(context.Sender);
				}
			}
		}
	}

	// When every connected client is frozen, don't send (or even build) state packets.
	// Changed indexes keep accumulating server-side (deduplicated, bounded by the number
	// of circuit states), so the first packet after unfreezing is a complete catch-up.
	// If only some clients are frozen, packets are still broadcast and frozen clients
	// buffer them locally instead.
	[HarmonyPatch(typeof(SenderShortcuts), nameof(SenderShortcuts.CircuitStatesUpdate))]
	public static class SkipStateBroadcastPatch
	{
		public static bool Prefix(bool force)
		{
			if (force)
			{
				return true;
			}
			int frozen;
			lock (EvesQoLServer.FrozenClients)
			{
				frozen = EvesQoLServer.FrozenClients.Count;
			}
			return frozen == 0 || frozen < EvesQoLServer.Network.ConnectionsCount;
		}
	}
}
