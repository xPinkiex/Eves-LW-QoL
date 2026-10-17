using System.Collections.Generic;
using System.Linq;
using LogicAPI.Data;
using LogicAPI.Services;
using LogicWorld;
using LogicWorld.Interfaces;
using LogicWorld.Physics;
using UnityEngine;

namespace Eves_QoL.Client
{
	public static class LineDetection
	{
		private const int MaxComponents = 1000;
		private const float SpacingTolerance = 0.001f;

		public static ComponentAddress[] DetectLine(ComponentAddress anchorA, ComponentAddress anchorB)
		{
			var data = Instances.MainWorld.Data;
			var compA = data.Lookup(anchorA);
			var compB = data.Lookup(anchorB);
			if (compA == null || compB == null)
			{
				return null;
			}

			var type = compA.Data.Type;
			var start = compA.WorldPosition;
			var end = compB.WorldPosition;
			var dir = (end - start).normalized;

			var result = new HashSet<ComponentAddress> { anchorA, anchorB };

			// Walk from A towards B using Physics.Raycast (via FindNextComponent).
			// ChunkCaster.CastAll was used before but missed the component adjacent
			// to the start anchor — Physics.Raycast from inside a collider correctly
			// detects the next collider entry, ChunkCaster does not.

			var currentPos = start;
			var counter = 0;
			while (counter++ < MaxComponents)
			{
				var addr = FindNextComponent(currentPos, dir, out var nextPos);
				if (addr == null || !data.Contains(addr.Value))
				{
					break;
				}
				if (addr.Value == anchorB)
				{
					break;
				}
				if (data.Lookup(addr.Value).Data.Type != type)
				{
					break;
				}
				result.Add(addr.Value);
				currentPos = nextPos.Value;
			}

			// --- The auto-expand logic below is adapted from CWPGroupAxis.expandInternal ---
			// --- (CWPGroupAxis.cs:400-486) and loopExpand (CWPGroupAxis.cs:491-529) ---
			// --- Again: Ecconia, sorry for kidnapping your code. It was too good not to steal. ---

			ExpandPast(end, dir, type, result, data);

			return result.ToArray();
		}

		private static void ExpandPast(Vector3 origin, Vector3 dir, ComponentType type, HashSet<ComponentAddress> result, IWorldData data)
		{
			var pos0 = origin;
			Vector3? pos1Nullable = null;

			var addr1 = FindNextComponent(origin, dir, out pos1Nullable);
			if (addr1 == null || !data.Contains(addr1.Value))
			{
				return;
			}
			if (data.Lookup(addr1.Value).Data.Type != type)
			{
				return;
			}
			result.Add(addr1.Value);
			var pos1 = pos1Nullable.Value;

			var addr2 = FindNextComponent(pos1, dir, out var pos2Nullable);
			if (addr2 == null || !data.Contains(addr2.Value))
			{
				return;
			}
			if (data.Lookup(addr2.Value).Data.Type != type)
			{
				return;
			}
			result.Add(addr2.Value);

			var pos2 = pos2Nullable.Value;
			var refDist = (pos1 - pos0).sqrMagnitude;
			var currentDist = (pos2 - pos1).sqrMagnitude;

			if (!IsSame(refDist, currentDist))
			{
				refDist = currentDist;
			}

			LoopExpand(pos2, dir, refDist, type, result, data);
		}

		private static void LoopExpand(Vector3 pos0, Vector3 dir, float refDist, ComponentType type, HashSet<ComponentAddress> result, IWorldData data)
		{
			var counter = 0;
			while (true)
			{
				var addr = FindNextComponent(pos0, dir, out var pos1Nullable);
				if (addr == null || !data.Contains(addr.Value))
				{
					return;
				}
				if (data.Lookup(addr.Value).Data.Type != type)
				{
					return;
				}
				var pos1 = pos1Nullable.Value;
				var dist = (pos1 - pos0).sqrMagnitude;

				if (!IsSame(refDist, dist))
				{
					return;
				}

				result.Add(addr.Value);
				pos0 = pos1;

				if (counter++ > MaxComponents)
				{
					return;
				}
			}
		}

		// Adapted from CWPHelper.findNextPeg (CWPHelper.cs:68-89).
		// Ecconia, sorry for kidnapping your code — the double-raycast center trick is yours.
		private static ComponentAddress? FindNextComponent(Vector3 start, Vector3 ray, out Vector3? position)
		{
			if (Physics.Raycast(start, ray, out var hitEnter, 30, Masks.Structure | Masks.Peg))
			{
				var collider = hitEnter.collider;
				var reference = ColliderReferenceDataTracker.GetReferenceFromColliderOrDefault(collider);
				var addr = reference.ComponentAddress;
				if (addr.IsEmpty())
				{
					var pegAddr = reference.PegAddress;
					if (!pegAddr.IsEmpty())
					{
						addr = pegAddr.ComponentAddress;
					}
				}
				if (addr.IsEmpty())
				{
					position = null;
					return null;
				}
				var entryPoint = hitEnter.point;
				if (!collider.Raycast(new Ray(entryPoint + ray * 3, -ray), out var hitExit, 30))
				{
					position = entryPoint + ray * 0.04f;
					return addr;
				}
				position = entryPoint + (hitExit.point - entryPoint) / 2;
				return addr;
			}
			position = null;
			return null;
		}

		private static bool IsSame(float a, float b)
		{
			return Mathf.Abs(a - b) < SpacingTolerance;
		}
	}
}