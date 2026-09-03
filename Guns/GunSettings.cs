using MelonLoader;
using UnityEngine;
#if IL2CPP
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Fields;
#endif

namespace MoreGuns.Guns
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class GunSettings : MonoBehaviour
    {
#if IL2CPP
        public Il2CppValueField<bool> isAutomatic;
        public Il2CppValueField<float> speedMultiplier;
        public Il2CppValueField<bool> cameraJolt;
        public Il2CppValueField<bool> requireWindup;
        public Il2CppValueField<float> windupTime;
        public Il2CppValueField<bool> canManualyReload;

        public GunSettings(IntPtr ptr) : base(ptr) { }

        public GunSettings() : base(ClassInjector.DerivedConstructorPointer<GunSettings>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
#else
        public bool isAutomatic;
        public float speedMultiplier;
        public bool cameraJolt;
        public bool requireWindup;
        public float windupTime;
        public bool canManualyReload;
#endif

        public void SetValues(bool automatic, float speed, bool jolt, bool windup, float windupSeconds, bool manualReload)
        {
#if IL2CPP
            isAutomatic.Value = automatic;
            speedMultiplier.Value = speed;
            cameraJolt.Value = jolt;
            requireWindup.Value = windup;
            windupTime.Value = windupSeconds;
            canManualyReload.Value = manualReload;
#else
            isAutomatic = automatic;
            speedMultiplier = speed;
            cameraJolt = jolt;
            requireWindup = windup;
            windupTime = windupSeconds;
            canManualyReload = manualReload;
#endif
        }

        public void CopyFrom(GunSettings other)
        {
            SetValues(other.isAutomatic, other.speedMultiplier, other.cameraJolt, other.requireWindup, other.windupTime, other.canManualyReload);
        }
    }
}
