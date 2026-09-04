namespace MoreGuns.Guns
{
    public class Sniper : WeaponBase
    {
        private static Sniper instance;
        public static Sniper Instance => instance;

        public Sniper()
        {
            if (instance != null)
                return;

            var tuning = new GunTuning();
            // Semi-auto + MustBeCocked (bolt) applied in WeaponBehavior.
            tuning.SetValues(false, 0.7F, true, false, 0.0F, true);

            Init("Sniper", "sniper", tuning);
            instance = this;
        }
    }
}
