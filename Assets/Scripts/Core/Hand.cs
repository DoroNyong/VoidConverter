using System;
using System.Collections.Generic;

namespace VoidConverter.Core
{
    /// <summary>The blocks currently offered to the player. Only used slots are refilled.</summary>
    public class Hand
    {
        const int MaxRerollAttempts = 50;

        public int Size => Slots.Length;
        public BlockShape[] Slots { get; }
        public int[] Rotations { get; }
        public int Selected { get; private set; } = -1;

        public Hand(int size)
        {
            Slots = new BlockShape[size];
            Rotations = new int[size];
        }

        /// <summary>
        /// Fills empty slots. When <paramref name="canPlace"/> is given and nothing in the hand fits the board,
        /// one of the newly drawn blocks is redrawn until something fits, so the player is never handed a dead hand
        /// while the board still has room.
        /// </summary>
        public void Fill(BlockSettings settings, Random rng, Func<BlockShape, bool> canPlace = null)
        {
            var filled = new List<int>();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null) continue;
                // Slots still holds the other blocks (and nulls), so they are excluded from the draw.
                Slots[i] = BlockLibrary.Pick(settings, rng, Slots);
                Rotations[i] = 0;
                filled.Add(i);
            }

            if (canPlace == null || filled.Count == 0 || AnyPlaceable(canPlace)) return;

            for (int attempt = 0; attempt < MaxRerollAttempts; attempt++)
            {
                int index = filled[rng.Next(filled.Count)];
                // Exclude the other blocks and the one being replaced, so the redraw is both new and not a duplicate.
                var avoid = new List<BlockShape>(Slots);
                Slots[index] = BlockLibrary.Pick(settings, rng, avoid);
                if (canPlace(Slots[index])) return;
            }
        }

        public bool AnyPlaceable(Func<BlockShape, bool> canPlace)
        {
            foreach (var shape in Slots)
            {
                if (shape != null && canPlace(shape)) return true;
            }
            return false;
        }

        public bool Select(int index)
        {
            if (index < 0 || index >= Slots.Length || Slots[index] == null) return false;
            Selected = index;
            return true;
        }

        /// <summary>Rotates the block in the given slot a quarter turn clockwise. Empty slots are ignored.</summary>
        public void Rotate(int index)
        {
            if (index < 0 || index >= Slots.Length || Slots[index] == null) return;
            Rotations[index] = (Rotations[index] + 1) % 4;
        }

        /// <summary>Puts the held block back without using it. Its rotation is kept.</summary>
        public void Deselect()
        {
            Selected = -1;
        }

        public Int2[] SelectedCells()
        {
            return Selected < 0 ? null : Slots[Selected].Rotated(Rotations[Selected]);
        }

        public Int2[] CellsOf(int index)
        {
            return Slots[index] == null ? null : Slots[index].Rotated(Rotations[index]);
        }

        /// <summary>Removes the selected block from the hand and returns it. Call Fill afterwards to refill.</summary>
        public BlockShape UseSelected()
        {
            if (Selected < 0) return null;
            var used = Slots[Selected];
            Slots[Selected] = null;
            Rotations[Selected] = 0;
            Selected = -1;
            return used;
        }
    }
}
