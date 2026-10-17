using EccsLogicWorldAPI.Server;
using LogicAPI.Server.Components;
using LogicLog;
using LogicWorld.SharedCode.ExtraData;

namespace Eves_QoL.Server
{
    public class BreakpointLogic : LogicComponent
    {
        private bool _prevInput;
        private static ILogicLogger _logger;

        protected override void Initialize()
        {
            _logger = LogicLogger.For<BreakpointLogic>();

            if (EvesQoLServerMod.PauseAccessor == null)
            {
                var extraData = ServiceGetter.getService<ExtraData>();
                EvesQoLServerMod.PauseAccessor = extraData.GetDataAccessor<bool>("MHG.SimulationPaused", false);
            }
        }

        protected override void DoLogicUpdate()
        {
            bool currentInput = Inputs[0].On;
            if (currentInput && !_prevInput)
            {
                EvesQoLServerMod.PauseAccessor?.SetData(true);
                _logger?.Info("Breakpoint hit! Simulation paused.");
            }
            _prevInput = currentInput;
        }

        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex)
        {
            return inputIndex == 0;
        }
    }
}