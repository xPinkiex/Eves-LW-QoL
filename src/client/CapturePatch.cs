using HarmonyLib;
using LogicWorld.Building.Overhaul;

namespace Eves_QoL.Client
{
	[HarmonyPatch(typeof(BuildingOperationsManager), nameof(BuildingOperationsManager.TryDoBuildingOperation))]
	public class CapturePatch
	{
		public static void Prefix(ComponentSelection selection)
		{
			SelectionMemory.SetPending(selection);
		}

		public static void Postfix()
		{
			SelectionMemory.Commit();
		}
	}

	[HarmonyPatch(typeof(DeletingOperation), nameof(DeletingOperation.BeginOperationOn))]
	public class DeleteClearPatch
	{
		public static void Prefix()
		{
			SelectionMemory.FlagSkip();
		}
	}
}