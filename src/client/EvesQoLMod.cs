using Eves_QoL.Client.Inputs;
using FancyInput;
using HarmonyLib;
using LogicAPI.Client;
using LogicWorld;
using LogicWorld.Building.Overhaul;
using LogicWorld.UI;

namespace Eves_QoL.Client
{
	public class EvesQoLMod : ClientMod
	{
		protected override void Initialize()
		{
			CustomInput.Register<ReSelectContext, ReSelectTriggers>("Eves_QoL");

			FirstPersonInteraction.RegisterBuildingKeybinding(
				ReSelectTriggers.RestoreSelection,
				TryRestoreSelection
			);

			FirstPersonInteraction.RegisterBuildingKeybinding(
				ReSelectTriggers.SelectLine,
				LineSelector.TryStartLineSelect
			);

			new Harmony("Eves_QoL").PatchAll();
		}

		private static bool TryRestoreSelection()
		{
			if (!SelectionMemory.TryGetNext(out var valid))
			{
				return false;
			}

			MultiSelector.StartWithSelection(new ComponentSelection(valid));
			return true;
		}
	}
}