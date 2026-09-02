using MelonLoader;
using System.Collections;
using UnityEngine;

namespace MoreGuns.Gui
{
    public static class Reticle
    {
        private const float RETRY_INTERVAL = 0.5F;
        private const float TIMEOUT = 30F;

        public static GameObject reticle;

        public static void Initialize()
        {
            reticle = null;
            MelonCoroutines.Start(FindAndInstantiateCrosshair());
        }

        public static IEnumerator FindAndInstantiateCrosshair()
        {
            float waited = 0F;

            while (HUD.Instance == null || HUD.Instance.crosshair == null)
            {
                if (waited >= TIMEOUT)
                {
                    MelonLogger.Warning("Gave up waiting for the HUD crosshair; MoreGuns will not show a custom reticle.");
                    yield break;
                }

                waited += RETRY_INTERVAL;
                yield return new WaitForSeconds(RETRY_INTERVAL);
            }

            reticle = UnityEngine.Object.Instantiate(HUD.Instance.crosshair.gameObject, HUD.Instance.crosshair.transform.parent);
            reticle.SetActive(false);
        }

        public static void SetActive(bool active)
        {
            if (reticle != null)
                reticle.SetActive(active);
        }
    }
}
