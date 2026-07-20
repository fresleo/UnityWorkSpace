using UnityEngine;

namespace GRTools.Localization
{
    public class LocalizationTextMesh : MonoBehaviour
    {
        [Tooltip("本地化键")] public int localizationKey;
        [Tooltip("本地化默认文本")] public string defaultValue;
        
         private TextMesh text;

        private void Start()
        {
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
            if (text == null)
            {
                text = GetComponent<TextMesh>();
            }
            if (text != null)
            {
                string value = LanguageMasterManager.Singleton.GetLocalizedText(localizationKey, defaultValue);
                text.text = value;
            }
        }
    }
}