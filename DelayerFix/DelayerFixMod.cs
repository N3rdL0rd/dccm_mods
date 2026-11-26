using dc.libs;
using HaxeProxy.Runtime;
using ModCore.Mods;
using Hashlink.Proxy.Objects;

namespace DelayerFix
{
    /// <summary>
    /// Dead Cells' <c>libs.Delayer.addF</c>, <c>addMs</c> and <c>addS</c> each push a task and then run a full stable
    /// merge sort of the whole queue on every call. Weapons like the Panchaku call it hundreds of times in one frame. The queue is
    /// always sorted before an add, so a single stable insertion gives the identical order.
    /// </summary>
    public unsafe class DelayerFixMod(ModInfo info) : ModBase(info)
    {
        private bool verified;
        private bool disabled;

        public override void Initialize()
        {
            Hook_Delayer.addF += Hook_addF;
            Hook_Delayer.addMs += Hook_addMs;
            Hook_Delayer.addS += Hook_addS;
        }

        // Due times exactly as in the bytecode:
        //   addF(frames):  now + frames
        //   addMs(ms):     now + ms / 1000.0 * fps
        //   addS(sec):     now + sec * fps
        private void Hook_addF(Hook_Delayer.orig_addF orig, Delayer self, dc.String id, HlAction cb, double frames)
        {
            if (!Add(self, id, cb, self.now + frames))
            {
                orig(self, id, cb, frames);
            }
        }

        private void Hook_addMs(Hook_Delayer.orig_addMs orig, Delayer self, dc.String id, HlAction cb, double ms)
        {
            if (!Add(self, id, cb, self.now + ms / 1000.0 * self.fps))
            {
                orig(self, id, cb, ms);
            }
        }

        private void Hook_addS(Hook_Delayer.orig_addS orig, Delayer self, dc.String id, HlAction cb, double sec)
        {
            if (!Add(self, id, cb, self.now + sec * self.fps))
            {
                orig(self, id, cb, sec);
            }
        }

        /// <summary>Returns false (nothing was pushed) when the caller must run the original method.</summary>
        private bool Add(Delayer self, dc.String id, HlAction cb, double due)
        {
            if (disabled)
            {
                return false;
            }

            var task = new dc.libs._Delayer.Task(id, due, cb);

            if (!verified)
            {
                // One-time sanity check of the assumed memory layout; fall back to the original on mismatch.
                if (DelayerQueue.ReadTime(task.HashlinkPointer) != task.t)
                {
                    disabled = true;
                    Logger.Error("DelayerFix: unexpected Task layout, mod disabled (original methods used).");
                    return false;
                }
                verified = true;
                Logger.Information("DelayerFix: active.");
            }

            var delays = self.delays;
            delays.push(task);

            // delays.array is the backing hl.NativeArray of object pointers; length is the logical size.
            DelayerQueue.InsertLast((nint*)delays.array.Data, delays.length);
            return true;
        }
    }
}
