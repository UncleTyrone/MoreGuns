namespace MoreGuns.Guns
{
    public class RPG : WeaponBase
    {
        private static RPG instance;
        public static RPG Instance => instance;

        public RPG()
        {
            if (instance != null)
                return;

            var tuning = new GunTuning();
            // syncMagazineToAmmo: hide/show rocket mesh with Ammo; explosiveRounds: blast on hit
            tuning.SetValues(false, 0.5F, true, false, 0.0F, true, syncMag: true, explosive: true);

            Init("RPG", "rpg", tuning);
            instance = this;
        }
    }
}
