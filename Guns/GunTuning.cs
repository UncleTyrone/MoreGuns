namespace MoreGuns.Guns
{
    /// <summary>
    /// Plain managed tuning — never store these flags on an unattached Il2Cpp MonoBehaviour.
    /// Il2CppValueField reads from a non-scene GunSettings return defaults and break auto/reload/RPG.
    /// </summary>
    public sealed class GunTuning
    {
        public bool isAutomatic;
        public float speedMultiplier = 1f;
        public bool cameraJolt;
        public bool requireWindup;
        public float windupTime;
        public bool canManualyReload = true;
        public bool syncMagazineToAmmo;
        public bool explosiveRounds;

        public void SetValues(
            bool automatic,
            float speed,
            bool jolt,
            bool windup,
            float windupSeconds,
            bool manualReload,
            bool syncMag = false,
            bool explosive = false)
        {
            isAutomatic = automatic;
            speedMultiplier = speed;
            cameraJolt = jolt;
            requireWindup = windup;
            windupTime = windupSeconds;
            canManualyReload = manualReload;
            syncMagazineToAmmo = syncMag;
            explosiveRounds = explosive;
        }
    }
}
