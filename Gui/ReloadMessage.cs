using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MoreGuns.Gui
{
    public static class ReloadMessage
    {
        private const float VISIBLE_SECONDS = 4F;

        public static GameObject message;
        public static Text messageText;

        private static object fadeCoroutine;

        public static void Initialize(Transform parent)
        {
            message = null;
            messageText = null;
            MelonCoroutines.Start(LoadAsset(parent));
        }

        public static IEnumerator LoadAsset(Transform parent)
        {
            var rqMessage = MoreGunsMod.assetBundle.LoadAssetAsync<GameObject>("assets/ui/Reload Message.prefab");
            yield return rqMessage;

            UnityEngine.Object UEOMessage = rqMessage.asset;
            if (UEOMessage == null)
            {
                MelonLogger.Error("Could not load the reload message prefab from the asset bundle.");
                yield break;
            }

            message = UnityEngine.Object.Instantiate(UEOMessage.As<GameObject>(), parent);
            message.SetActive(false);
            messageText = message.GetComponentInChildren<Text>(true);
        }

        public static void Show(bool show)
        {
            if (message == null)
                return;

            message.SetActive(show);

            if (fadeCoroutine != null)
            {
                MelonCoroutines.Stop(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (show)
                fadeCoroutine = MelonCoroutines.Start(Fade(message));
        }

        public static void Show(string text)
        {
            SetText(text);
            Show(true);
        }

        public static IEnumerator Fade(GameObject o)
        {
            yield return new WaitForSeconds(VISIBLE_SECONDS);
            if (o != null)
                o.SetActive(false);
            fadeCoroutine = null;
        }

        public static void SetText(string text)
        {
            if (messageText != null)
                messageText.text = text;
        }
    }
}
