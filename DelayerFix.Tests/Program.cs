using System.Runtime.InteropServices;
using DelayerFix;

// Fake HL task objects in native memory: [type ptr (8 bytes)][t: double] -> t at offset 8, like libs._Delayer.Task.
// Compares InsertLast against a stable sort (LINQ OrderBy is stable) over mixed random inserts and removals.
static unsafe class Tests
{
    static nint NewTask(double t, List<(nint p, int seq)> all)
    {
        var p = (nint)NativeMemory.AllocZeroed(24);
        *(double*)(p + 8) = t;
        all.Add((p, all.Count));
        return p;
    }

    static int Main()
    {
        int failures = 0;
        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new Random(seed);
            var all = new List<(nint p, int seq)>();
            const int cap = 4096;
            var items = (nint*)NativeMemory.AllocZeroed((nuint)(cap * sizeof(nint)));
            int count = 0;
            var reference = new List<nint>(); // stable sort of full list after every add, like the original
            int ops = seed == 0 ? 878 : 300 + rng.Next(600);
            for (int op = 0; op < ops; op++)
            {
                if (count > 0 && rng.Next(5) == 0)
                {
                    // pop front (what Delayer.update does when a task fires)
                    for (int i = 1; i < count; i++) items[i - 1] = items[i];
                    count--;
                    reference.RemoveAt(0);
                    continue;
                }
                // small integer range => many equal timestamps, exercises stability
                double t = rng.Next(3) == 0 ? rng.Next(0, 30) : rng.NextDouble() * 100;
                var task = NewTask(t, all);
                items[count++] = task;
                DelayerQueue.InsertLast(items, count);
                reference.Add(task);
                reference = reference.OrderBy(x => *(double*)(x + 8)).ToList(); // stable
                for (int i = 0; i < count; i++)
                {
                    if (items[i] != reference[i]) { failures++; Console.WriteLine($"MISMATCH seed={seed} op={op} idx={i}"); goto next; }
                }
            }
            next:
            NativeMemory.Free(items);
            foreach (var (p, _) in all) NativeMemory.Free((void*)p);
        }

        // edge cases
        {
            var items = stackalloc nint[1];
            DelayerQueue.InsertLast(items, 0);
            DelayerQueue.InsertLast(items, 1);
        }
        Console.WriteLine(failures == 0 ? "PASS: 200 randomized runs (incl. 878-insert run) identical to stable sort" : $"FAIL: {failures}");
        return failures == 0 ? 0 : 1;
    }
}
