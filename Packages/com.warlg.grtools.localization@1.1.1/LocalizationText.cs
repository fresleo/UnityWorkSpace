using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GRTools.Localization
{
    /**
     *  通过  textmeshpro 和 textmeshprougui
     */

    public class LocalizationText : MonoBehaviour
    {
        [Tooltip("本地化键")] public int localizationKey;
        // [Tooltip("本地化默认文本")] public string defaultValue ;

        private TMP_Text text;
        private Text defaultText;

        private void Start()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }

            if (defaultText == null)
            {
                defaultText = GetComponent<Text>();
            }
            if (LanguageMasterManager.Singleton != null && LanguageMasterManager.Singleton.CurrentLocalizationInfo != null)
            {
                OnLocalizationChanged(LanguageMasterManager.Singleton.CurrentLocalizationInfo);
            }
            LanguageMasterManager.LocalizationChangeEvent += OnLocalizationChanged;
        }

        private void OnDestroy()
        {
            LanguageMasterManager.LocalizationChangeEvent -= OnLocalizationChanged;
        }

        private void OnLocalizationChanged(LocalizationInfo localizationInfo)
        {
            if (localizationKey <= 0)
            {
                return;
            }
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }
            if (defaultText == null)
            {
                defaultText = GetComponent<Text>();
            }

            if (defaultText)
            {
                defaultText.text = LanguageMasterManager.Singleton.GetLocalizedText(localizationKey);
            }

            if (text != null)
            {
                LanguageMasterManager.Singleton.LoadLocalizationAssetAsync<TMP_FontAsset>(localizationInfo.AssetsPath, (font) =>
                {
                    if (font == null)
                    {
                        Debug.LogError("字体加载错误");
                        return;
                    }

                    text.font = font;
                });
                string value = LanguageMasterManager.Singleton.GetLocalizedText(localizationKey);
                text.text = value;
            }
        }
    }
}