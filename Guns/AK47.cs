namespace MoreGuns.Guns
{
    public class AK47 : WeaponBase
    {
        private static AK47 instance;
        public static AK47 Instance => instance;

        public AK47()
        {
            if (instance != null)
                return;

            var tuning = new GunTuning();
            tuning.SetValues(true, 1.0F, true, false, 0.0F, true);

            Init("AK47", "ak47", tuning);
            instance = this;
        }
    }
}
