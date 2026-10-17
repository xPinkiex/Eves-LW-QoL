using Eves_QoL.Client.Inputs;
using FancyInput;
using LogicWorld.Building.Overhaul;
using LogicWorld.GameStates;
using LogicWorld.UI;

namespace Eves_QoL.Client
{
	public class RestoreSelectionOperation : BuildingOperation
	{
		public override InputTrigger OperationStarter => ReSelectTriggers.RestoreSelection;

		public override bool CanOperateOn(ComponentSelection selection)
		{
			SelectionMemory.FlagSkip();
			return SelectionMemory.TryGetNext(out var _);
		}

		public override void BeginOperationOn(ComponentSelection selection)
		{
			var valid = SelectionMemory.GetLastValidResult();
			if (valid == null || valid.Length == 0)
			{
				return;
			}

			GameStateManager.TransitionBackToBuildingState();
			MultiSelector.StartWithSelection(new ComponentSelection(valid));
		}
	}
}