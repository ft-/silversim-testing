// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System.Diagnostics;

namespace SilverSim.Threading
{
    public abstract partial class TimeProvider
    {
        public abstract long TickCount { get; }
        public abstract long Frequency { get; }

        public abstract long SecsToTicks(long secs);
        public abstract long MsecsToTicks(long millisecs);
        public abstract double NormalizedToEventsPerSeconds(int events, long deltaticks);
        public abstract long TicksElapsed(long newts, long oldts);
        public abstract double TicksToSecs(long ticks);
        public abstract double TicksToMsecs(long ticks);
    }

    public sealed class StopWatchSource : TimeProvider
    {
        public override long TickCount => Stopwatch.GetTimestamp();
        public override long Frequency => Stopwatch.Frequency;

        public override long SecsToTicks(long secs) => secs * Stopwatch.Frequency;
        public override long MsecsToTicks(long millisecs) => millisecs * Stopwatch.Frequency / 1000;
        public override double NormalizedToEventsPerSeconds(int events, long deltaticks) => events * Stopwatch.Frequency / (double)deltaticks;
        public override long TicksElapsed(long newts, long oldts) => newts - oldts;
        public override double TicksToSecs(long ticks) => ticks / (double)Stopwatch.Frequency;
        public override double TicksToMsecs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    }

    public sealed class EnvironmentSource : TimeProvider
    {
        public override long TickCount => System.Environment.TickCount;
        public override long Frequency => 1000;

        public override long SecsToTicks(long secs) => secs * 1000;
        public override long MsecsToTicks(long millisecs) => millisecs;
        public override double NormalizedToEventsPerSeconds(int events, long deltaticks) => events * 1000 / (double)deltaticks;
        public override long TicksElapsed(long newts, long oldts) => (int)newts - (int)oldts;
        public override double TicksToSecs(long ticks) => ticks / 1000.0;
        public override double TicksToMsecs(long ticks) => ticks;
    }

    public abstract partial class TimeProvider
    {
        public static readonly TimeProvider StopWatch = new StopWatchSource();
        public static readonly TimeProvider Environment = new EnvironmentSource();
    }
}
