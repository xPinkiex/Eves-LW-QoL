using LogicAPI.Data;
using LogicWorld;
using LogicWorld.Building.Overhaul;
using LogicWorld.Physics;
using LogicWorld.Players;
using LogicWorld.UI;

namespace Eves_QoL.Client
{
	public static class LineSelector
	{
		private static ComponentAddress anchorA;

		public static bool TryStartLineSelect()
		{
			var hit = PlayerCaster.CameraCast(Masks.Structure | Masks.Peg | Masks.Environment);
			if (!hit.HitComponent)
			{
				anchorA = default;
				return false;
			}

			if (anchorA.IsEmpty())
			{
				anchorA = hit.cAddress;
				return true;
			}

			var anchorB = hit.cAddress;
			if (anchorB == anchorA)
			{
				anchorA = default;
				return false;
			}

			var addresses = LineDetection.DetectLine(anchorA, anchorB);
			anchorA = default;

			if (addresses == null || addresses.Length == 0)
			{
				return false;
			}

			var selection = new ComponentSelection(addresses).FilteredToUniqueHierarchies();
			if (selection.Count == 0)
			{
				return false;
			}

			MultiSelector.StartWithSelection(selection);
			return true;
		}
	}
}