using System;

namespace VoidConverter.Core
{
    /// <summary>The blocks currently offered to the player. Only used slots are refilled.</summary>
    public class Hand
    {
        public int Size => Slots.Length;
        public BlockShape[] Slots { get; }
        public int[] Rotations { get; }
        public int Selected { get; private set; } = -1;

        public Hand(int size)
        {
            Slots = new BlockShape[size];
            Rotations = new int[size];
        }

        public void Fill(BlockSettings settings, Random rng)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null) continue;
                Slots[i] = BlockLibrary.Pick(settings, rng);
                Rotations[i] = 0;
            }
        }

        public bool Select(int index)
        {
            if (index < 0 || index >= Slots.Length || Slots[index] == null) return false;
            Selected = index;
            return true;
        }

        public void Rotate()
        {
            if (Selected < 0) return;
            Rotations[Selected] = (Rotations[Selected] + 1) % 4;
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
