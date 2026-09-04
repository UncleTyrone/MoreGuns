namespace MoreGuns.Guns
{
    public class MiniGun : WeaponBase
    {
        private static MiniGun instance;
        public static MiniGun Instance => instance;

        public MiniGun()
        {
            if (instance != null)
                return;

            var tuning = new GunTuning();
            tuning.SetValues(true, 0.75F, false, true, 2.0F, Config.AllowMinigunManualReload.Value);

            Init("MiniGun", "minigun", tuning);
            instance = this;
        }
    }
}
