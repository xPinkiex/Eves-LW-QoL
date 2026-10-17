using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicWorld;
using LogicWorld.Building.Overhaul;
using LogicWorld.Interfaces;

namespace Eves_QoL.Client
{
	public static class SelectionMemory
	{
		private const int MaxHistory = 5;

		private static readonly List<ComponentAddress[]> history = new List<ComponentAddress[]>();
		private static int cycleIndex = -1;

		private static ComponentAddress[] pending;
		private static bool skipThisCapture;

		private static ComponentAddress[] lastValidResult;

		public static void SetPending(ComponentSelection selection)
		{
			pending = null;
			skipThisCapture = false;
			if (selection == null || selection.Count == 0)
			{
				return;
			}
			pending = selection.ToArray();
		}

		public static void FlagSkip()
		{
			skipThisCapture = true;
		}

		public static void Commit()
		{
			if (skipThisCapture || pending == null || pending.Length == 0)
			{
				pending = null;
				return;
			}

			if (history.Count > 0 && AreSameAddresses(history[history.Count - 1], pending))
			{
				pending = null;
				return;
			}

			history.Add(pending);
			if (history.Count > MaxHistory)
			{
				history.RemoveAt(0);
			}

			cycleIndex = history.Count - 1;
			pending = null;
		}

		public static bool TryGetNext(out ComponentAddress[] validAddresses)
		{
			validAddresses = null;
			var data = Instances.MainWorld.Data;

			while (cycleIndex >= 0)
			{
				var entry = history[cycleIndex];
				cycleIndex--;

				var valid = entry.Where(a => data.Contains(a)).ToArray();
				if (valid.Length > 0)
				{
					validAddresses = valid;
					lastValidResult = valid;
					return true;
				}
			}

			return false;
		}

		public static ComponentAddress[] GetLastValidResult()
		{
			return lastValidResult;
		}

		private static bool AreSameAddresses(ComponentAddress[] a, ComponentAddress[] b)
		{
			if (a.Length != b.Length)
			{
				return false;
			}
			var set = new HashSet<ComponentAddress>(a);
			foreach (var addr in b)
			{
				if (!set.Remove(addr))
				{
					return false;
				}
			}
			return true;
		}
	}
}