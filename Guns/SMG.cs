namespace MoreGuns.Guns
{
    public class SMG : WeaponBase
    {
        private static SMG instance;
        public static SMG Instance => instance;

        public SMG()
        {
            if (instance != null)
                return;

            var tuning = new GunTuning();
            // Automatic, faster cadence than AK (FireCooldown tuned in WeaponBehavior).
            tuning.SetValues(true, 1.0F, false, false, 0.0F, true);

            Init("SMG", "smg", tuning);
            instance = this;
        }
    }
}
