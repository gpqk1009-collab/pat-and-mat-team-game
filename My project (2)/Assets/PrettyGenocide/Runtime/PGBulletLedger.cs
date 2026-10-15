using System;
using System.Collections.Generic;

namespace PrettyGenocide
{
    /// <summary>Every issued bullet owns one chamber until that exact ID is recovered.</summary>
    public sealed class PGBulletLedger
    {
        private readonly HashSet<int> outstanding = new HashSet<int>();
        private int nextId;
        public int Capacity { get; }
        public int Loaded => Capacity - outstanding.Count;
        public int Outstanding => outstanding.Count;

        public PGBulletLedger(int capacity = 6)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        public bool TryIssue(out int id)
        {
            id = -1;
            if (Loaded == 0) return false;
            do { nextId = unchecked(nextId + 1); } while (outstanding.Contains(nextId));
            id = nextId;
            outstanding.Add(id);
            return true;
        }

        public bool TryRecover(int id) => outstanding.Remove(id);
    }
}
