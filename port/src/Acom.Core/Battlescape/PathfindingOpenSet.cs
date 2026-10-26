/*
 * Copyright 2010-2026 OpenXcom Developers.
 * C# port: Copyright 2026 ACOM contributors.
 *
 * This file is part of ACOM, a C# port of OpenXcom Extended.
 *
 * ACOM is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ACOM is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Acom.  If not, see <http://www.gnu.org/licenses/>.
 */
// Ported from: src/Battlescape/PathfindingOpenSet.h, src/Battlescape/PathfindingOpenSet.cpp
#nullable disable

using System.Diagnostics;
using System.Numerics;

namespace Acom;

/// <summary>Entry of the open set queue.</summary>
public struct OpenSetEntry
{
	public PathfindingNode _node;
	public short _cost;
	public byte _openentry;
}

/// <summary>Comparator of open set entries (C++ functor).</summary>
public static class EntryCompare
{
	/// <summary>Compares entries a and b; true if entry b must come before a.</summary>
	public static bool Compare(OpenSetEntry a, OpenSetEntry b) => b._cost < a._cost;
}

/// <summary>
/// Priority queue of pathfinding nodes. Entries of nodes that were pushed again are discarded lazily.
/// </summary>
public class PathfindingOpenSet
{
	/// <summary>std::priority_queue&lt;OpenSetEntry, std::vector&lt;OpenSetEntry&gt;, EntryCompare&gt; (libstdc++ binary heap).</summary>
	private readonly List<OpenSetEntry> _queue = [];

	/// <summary>Removes reachable discarded entries.</summary>
	private void RemoveDiscarded()
	{
		while (_queue.Count != 0 && _queue[0]._node._openentry != _queue[0]._openentry)
		{
			StdAlgorithm.PopHeap(_queue, EntryCompare.Compare);
		}
	}

	/// <summary>Gets the next node to check.</summary>
	public PathfindingNode Pop()
	{
		Debug.Assert(!Empty());

		PathfindingNode nd = _queue[0]._node;
		StdAlgorithm.PopHeap(_queue, EntryCompare.Compare);
		nd._openentry = 0;

		// Discarded entries might be visible now.
		RemoveDiscarded();
		return nd;
	}

	/// <summary>Adds a node to the set.</summary>
	public void Push(PathfindingNode node)
	{
		Debug.Assert(node._openentry != 255);

		OpenSetEntry entry = default;
		entry._node = node;
		entry._cost = unchecked((short)(node.GetTUCost(false).time * 4 + node.TUGuess)); //HACK: this is not real cost, more rough approximation for algorithm, as bonus `getTUGuess` work more like gravity/potential than normal cost.
		entry._openentry = ++node._openentry; // next unique number, used to check if old recode is still valid.
		StdAlgorithm.PushHeap(_queue, entry, EntryCompare.Compare);
	}

	/// <summary>Is the set empty?</summary>
	public bool Empty() => _queue.Count == 0;
}

/// <summary>
/// The libstdc++ (GCC) heap and sort algorithms step by step, so that equal or incomparable elements end up in the
/// same order as in the original (std::priority_queue in the open set, std::sort in Pathfinding::findReachable).
/// <c>comp</c> is the C++ "less" comparator.
/// </summary>
internal static class StdAlgorithm
{
	/// <summary>libstdc++ <c>_S_threshold</c>.</summary>
	private const int S_threshold = 16;

	/// <summary>priority_queue::push: push_back + std::push_heap.</summary>
	public static void PushHeap<T>(List<T> heap, T value, Func<T, T, bool> comp)
	{
		heap.Add(value);
		PushHeapHole(heap, 0, heap.Count - 1, 0, value, comp);
	}

	/// <summary>priority_queue::pop: std::pop_heap + pop_back.</summary>
	public static void PopHeap<T>(List<T> heap, Func<T, T, bool> comp)
	{
		int last = heap.Count;
		if (last > 1)
		{
			--last;
			PopHeap(heap, 0, last, last, comp);
		}
		heap.RemoveAt(heap.Count - 1);
	}

	/// <summary>std::sort (introsort, not stable).</summary>
	public static void Sort<T>(List<T> a, Func<T, T, bool> comp)
	{
		int first = 0, last = a.Count;
		if (first != last)
		{
			IntrosortLoop(a, first, last, BitOperations.Log2((uint)(last - first)) * 2, comp);
			FinalInsertionSort(a, first, last, comp);
		}
	}

	/// <summary>std::__push_heap.</summary>
	private static void PushHeapHole<T>(List<T> a, int first, int holeIndex, int topIndex, T value, Func<T, T, bool> comp)
	{
		int parent = (holeIndex - 1) / 2;
		while (holeIndex > topIndex && comp(a[first + parent], value))
		{
			a[first + holeIndex] = a[first + parent];
			holeIndex = parent;
			parent = (holeIndex - 1) / 2;
		}
		a[first + holeIndex] = value;
	}

	/// <summary>std::__adjust_heap.</summary>
	private static void AdjustHeap<T>(List<T> a, int first, int holeIndex, int len, T value, Func<T, T, bool> comp)
	{
		int topIndex = holeIndex;
		int secondChild = holeIndex;
		while (secondChild < (len - 1) / 2)
		{
			secondChild = 2 * (secondChild + 1);
			if (comp(a[first + secondChild], a[first + (secondChild - 1)]))
				secondChild--;
			a[first + holeIndex] = a[first + secondChild];
			holeIndex = secondChild;
		}
		if ((len & 1) == 0 && secondChild == (len - 2) / 2)
		{
			secondChild = 2 * (secondChild + 1);
			a[first + holeIndex] = a[first + (secondChild - 1)];
			holeIndex = secondChild - 1;
		}
		PushHeapHole(a, first, holeIndex, topIndex, value, comp);
	}

	/// <summary>std::__pop_heap.</summary>
	private static void PopHeap<T>(List<T> a, int first, int last, int result, Func<T, T, bool> comp)
	{
		T value = a[result];
		a[result] = a[first];
		AdjustHeap(a, first, 0, last - first, value, comp);
	}

	/// <summary>std::__make_heap.</summary>
	private static void MakeHeap<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		if (last - first < 2)
			return;

		int len = last - first;
		int parent = (len - 2) / 2;
		while (true)
		{
			T value = a[first + parent];
			AdjustHeap(a, first, parent, len, value, comp);
			if (parent == 0)
				return;
			parent--;
		}
	}

	/// <summary>std::__sort_heap.</summary>
	private static void SortHeap<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		while (last - first > 1)
		{
			--last;
			PopHeap(a, first, last, last, comp);
		}
	}

	/// <summary>std::__heap_select.</summary>
	private static void HeapSelect<T>(List<T> a, int first, int middle, int last, Func<T, T, bool> comp)
	{
		MakeHeap(a, first, middle, comp);
		for (int i = middle; i < last; ++i)
			if (comp(a[i], a[first]))
				PopHeap(a, first, middle, i, comp);
	}

	/// <summary>std::__introsort_loop.</summary>
	private static void IntrosortLoop<T>(List<T> a, int first, int last, int depthLimit, Func<T, T, bool> comp)
	{
		while (last - first > S_threshold)
		{
			if (depthLimit == 0)
			{
				// std::__partial_sort(first, last, last)
				HeapSelect(a, first, last, last, comp);
				SortHeap(a, first, last, comp);
				return;
			}
			--depthLimit;
			int cut = UnguardedPartitionPivot(a, first, last, comp);
			IntrosortLoop(a, cut, last, depthLimit, comp);
			last = cut;
		}
	}

	/// <summary>std::__unguarded_partition_pivot.</summary>
	private static int UnguardedPartitionPivot<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		int mid = first + (last - first) / 2;
		MoveMedianToFirst(a, first, first + 1, mid, last - 1, comp);
		return UnguardedPartition(a, first + 1, last, first, comp);
	}

	/// <summary>std::__move_median_to_first.</summary>
	private static void MoveMedianToFirst<T>(List<T> a, int result, int x, int y, int z, Func<T, T, bool> comp)
	{
		if (comp(a[x], a[y]))
		{
			if (comp(a[y], a[z]))
				Swap(a, result, y);
			else if (comp(a[x], a[z]))
				Swap(a, result, z);
			else
				Swap(a, result, x);
		}
		else if (comp(a[x], a[z]))
			Swap(a, result, x);
		else if (comp(a[y], a[z]))
			Swap(a, result, z);
		else
			Swap(a, result, y);
	}

	/// <summary>std::__unguarded_partition (needs an irreflexive, asymmetric comparator, like the C++ code).</summary>
	private static int UnguardedPartition<T>(List<T> a, int first, int last, int pivot, Func<T, T, bool> comp)
	{
		while (true)
		{
			while (comp(a[first], a[pivot]))
				++first;
			--last;
			while (comp(a[pivot], a[last]))
				--last;
			if (!(first < last))
				return first;
			Swap(a, first, last);
			++first;
		}
	}

	/// <summary>std::__final_insertion_sort.</summary>
	private static void FinalInsertionSort<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		if (last - first > S_threshold)
		{
			InsertionSort(a, first, first + S_threshold, comp);
			// std::__unguarded_insertion_sort
			for (int i = first + S_threshold; i != last; ++i)
				UnguardedLinearInsert(a, first, i, comp);
		}
		else
		{
			InsertionSort(a, first, last, comp);
		}
	}

	/// <summary>std::__insertion_sort.</summary>
	private static void InsertionSort<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		if (first == last)
			return;

		for (int i = first + 1; i != last; ++i)
		{
			if (comp(a[i], a[first]))
			{
				T val = a[i];
				for (int j = i; j > first; --j)
					a[j] = a[j - 1];
				a[first] = val;
			}
			else
			{
				UnguardedLinearInsert(a, first, i, comp);
			}
		}
	}

	/// <summary>
	/// std::__unguarded_linear_insert. The extra stop at <paramref name="first"/> only matters where the C++ code would
	/// read before the range (undefined behaviour, possible with comparators that are not a strict weak ordering).
	/// </summary>
	private static void UnguardedLinearInsert<T>(List<T> a, int first, int last, Func<T, T, bool> comp)
	{
		T val = a[last];
		int next = last - 1;
		while (next >= first && comp(val, a[next]))
		{
			a[last] = a[next];
			last = next;
			--next;
		}
		a[last] = val;
	}

	private static void Swap<T>(List<T> a, int i, int j) => (a[i], a[j]) = (a[j], a[i]);
}
