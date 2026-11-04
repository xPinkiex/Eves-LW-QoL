using LogicAPI.Networking.Packets;
using MessagePack;

namespace Eves_QoL.Shared
{
	// Client -> server: stop/resume sending circuit state updates to this client.
	[MessagePackObject]
	public class WireFreezeRequest : Packet
	{
		[Key(0)] public bool Frozen;
	}
}
