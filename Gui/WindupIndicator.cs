using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MoreGuns.Gui
{
    public static class WindupIndicator
    {
        public static GameObject windupIndicator;
        public static Slider windupIndicatorSlider;
        public static Image backgroundImage;
        public static Image fillImage;

        public static void Initialize(Transform parent)
        {
            windupIndicator = null;
            windupIndicatorSlider = null;
            MelonCoroutines.Start(LoadAsset(parent));
        }

        public static IEnumerator LoadAsset(Transform parent)
        {
            var rqWindupIndicator = MoreGunsMod.assetBundle.LoadAssetAsync<GameObject>("assets/ui/Windup Indicator.prefab");
            yield return rqWindupIndicator;

            UnityEngine.Object UEOWindupIndicator = rqWindupIndicator.asset;
            if (UEOWindupIndicator == null)
            {
                MelonLogger.Error("Could not load the windup indicator prefab from the asset bundle.");
                yield break;
            }

            windupIndicator = UnityEngine.Object.Instantiate(UEOWindupIndicator.As<GameObject>(), parent);
            windupIndicator.SetActive(false);

            windupIndicatorSlider = windupIndicator.GetComponent<Slider>();
            backgroundImage = windupIndicator.transform.GetChild(0).GetComponent<Image>();
            fillImage = windupIndicator.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        }

        public static void Show(bool shown)
        {
            if (windupIndicator != null && windupIndicator.activeSelf != shown)
                windupIndicator.SetActive(shown);
        }

        public static void SetValueByTime(float from, float to)
        {
            if (to <= 0F || from >= to)
            {
                SetValue(100);
            }
            else
            {
                SetValue((int)((from * 100) / to));
            }
        }

        public static void SetValue(int value)
        {
            if (windupIndicatorSlider == null)
                return;

            windupIndicatorSlider.value = value;
            Show(value != 100);
        }
    }
}
