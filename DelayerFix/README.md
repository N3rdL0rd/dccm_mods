# DelayerFix: Panchaku combo lag fix for Dead Cells (DCCM mod)

## What it fixes
Hitting with the **Panchaku** (`tool.weap.NunchuckPan`) on its 8th and 9th strikes causes a visible lag spike
(15-30 ms per strike, worse on slower machines).

## Cause
The Panchaku schedules about 400 delayed checks in a single frame (6 per opponent) through
`libs.Delayer.addF`. In the game bytecode, `addF` does this (`addMs` and `addS` each contain their own inlined copy
of the same push-then-sort, differing only in how the due time is computed):

```
this.delays.push(new Task(id, this.now + frames, cb));
haxe.ds.ArraySort.sort(this.delays, this.cmp);   // full stable merge sort of the whole queue, on EVERY add
```

so 400 adds mean 400 full sorts.

## The fix
The queue is already sorted before every add, so the mod hooks `libs.Delayer.addF`, `libs.Delayer.addMs` and `libs.Delayer.addS` (all three share one
implementation) and replaces the sort with one
stable insertion: push the task, then starting at the last element swap it leftwards while
`cur.t < prev.t` (strict, so equal timestamps keep insertion order). The resulting order is identical to the
original stable sort. It works directly on the array's native pointers, with no per-element marshalling and no
allocation beyond the Task the original also creates.

Verified outside the game: the shared insertion (used by all three methods) produces the same order as a stable sort over 200 randomized runs with
duplicate timestamps and interleaved pops (including an 878-insert run).

## Install
Requires Dead Cells Core Modding (DCCM) installed. Unzip this archive into the game root so you get:

```
<DeadCellsGameRoot>/coremod/mods/DelayerFix/DelayerFix.dll
<DeadCellsGameRoot>/coremod/mods/DelayerFix/modinfo.json
```

To uninstall, delete the `DelayerFix` folder.

## Built for
- Dead Cells v35, Steam build
- bytecode sha256 `59350cad475a2375b9e39d7cd13fdb6e4ed9a027fd827ae5140f913c1c07ce7e`
- git hash `8b5231cd`
- DCCM core 35.9.23 (MDK 1.0.1), .NET 10

## Known limits
- Written against v35 field layouts (`Task.t` is read at byte offset 8 of the object). On first use the mod checks
  that layout against the proxy value; on mismatch it logs an error and falls back to the original `addF`.
- Only `addF`, `addMs` and `addS` are changed. Other `Delayer` code is untouched.
- The mod replaces these methods instead of wrapping them: another mod hooking the same methods at lower priority
  will not see the original run.
- The same algorithm was verified in game with the hlmod framework: queue order identical to the original stable
  sort for addF, addMs and addS over mixed inserts, and the Panchaku combo went from 15-30 ms strikes to smooth.

## Building from source
Needs the .NET 10 SDK, the DCCM MDK (`DeadCellsCoreModding.MDK` 1.0.1) and the `DCCM_MDK_ROOT` environment
variable pointing at the MDK folder (`coremod/core/mdk` of a DCCM install):

```
dotnet build -c Release     # output: bin/Release/net10.0/output/DelayerFix/
```

## Credit
Found by profiling the bytecode with crashlink and the hlmod framework; ported to DCCM here.
Thanks to the Dead Cells Core Modding team for the loader and MDK.
