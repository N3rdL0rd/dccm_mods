namespace DelayerFix
{
    /// <summary>
    /// Pure pointer-level logic, free of any game dependency so it can be unit tested over plain native memory.
    /// </summary>
    internal static unsafe class DelayerQueue
    {
        /// <summary>Byte offset of <c>Task.t</c> (first field, F64) inside a HashLink object: right after the type pointer.</summary>
        public const int TaskTimeOffset = 8;

        public static double ReadTime(nint task) => *(double*)(task + TaskTimeOffset);

        /// <summary>
        /// Restores sort order after a new element was appended at <c>items[count-1]</c>, assuming
        /// <c>items[0..count-1)</c> was already sorted by <c>Task.t</c>.
        /// Strict '&lt;' keeps equal timestamps in insertion order, which is exactly what the
        /// stable merge sort of the original code produced.
        /// </summary>
        public static void InsertLast(nint* items, int count)
        {
            int i = count - 1;
            if (i <= 0)
            {
                return;
            }
            nint cur = items[i];
            double t = ReadTime(cur);
            while (i > 0)
            {
                nint prev = items[i - 1];
                if (!(t < ReadTime(prev)))
                {
                    break;
                }
                items[i] = prev;
                i--;
            }
            items[i] = cur;
        }
    }
}
