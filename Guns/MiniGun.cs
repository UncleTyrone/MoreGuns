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

            GunSettings gunSettings = new GunSettings();
            gunSettings.SetValues(true, 0.75F, false, true, 2.0F, Config.AllowMinigunManualReload.Value);

            Init("MiniGun", "minigun", gunSettings);
            instance = this;
        }
    }
}
