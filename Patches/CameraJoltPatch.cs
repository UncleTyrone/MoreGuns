namespace MoreGuns.Patches
{
    // Held-weapon id is updated from GunSettings. Camera jolt is no longer Harmony-hooked
    // at startup; JoltCamera is left vanilla so MonoMod does not detour it.
    public static class CameraJoltPatch
    {
        public static string ID = "";
    }
}
