using System;

namespace ProjectSS.Expedition
{
    /// <summary>Hold-to-mine cadence. Releasing never consumes resources or continues an old hold.</summary>
    public sealed class MiningRhythm
    {
        public bool Holding { get; private set; }
        public float Heat { get; private set; }
        public float BurstRemaining { get; private set; }
        public int Combo { get; private set; }
        public int Charge { get; private set; }
        public float Interval => BurstRemaining > 0 ? 0.075f : 0.23f - Heat * 0.12f;
        public int BonusDamage => BurstRemaining > 0 ? 1 : 0;
        public void SetHeld(bool held)
        {
            Holding = held;
            if (!held) { Heat = 0; Combo = 0; Charge = 0; BurstRemaining = 0; }
        }
        public void Tick(float dt)
        {
            if (!Holding) return;
            Heat = Math.Min(1, Heat + dt / 2.4f);
            BurstRemaining = Math.Max(0, BurstRemaining - dt);
        }
        public bool BreakRock()
        {
            if (!Holding) return false;
            Combo++;
            if (BurstRemaining > 0) return false;
            Charge++;
            if (Charge < 6) return false;
            Charge = 0; BurstRemaining = 2.6f;
            return true;
        }
    }
}
