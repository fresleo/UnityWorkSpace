using UnityEngine;
using UnityEngine.UI;

namespace GRTools.Localization
{
    public class LocalizationSpriteRender : MonoBehaviour
    {
        [Tooltip("本地化键")] public int localizationKey;
        [Tooltip("本地化默认图片加载路径")] public string defaultValue;
        
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Start()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
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
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            string value = LanguageMasterManager.Singleton.GetLocalizedText(localizationKey, defaultValue);
            if (spriteRenderer != null && !string.IsNullOrEmpty(value))
            {
                LanguageMasterManager.Singleton.LoadLocalizationAssetAsync<Sprite>(value, sprite =>
                {
                    if (sprite == null && !string.IsNullOrEmpty(defaultValue) && value != defaultValue)
                    {
                        LanguageMasterManager.Singleton.LoadLocalizationAssetAsync<Sprite>(defaultValue,
                            defaultSprite =>
                            {
                                spriteRenderer.sprite = defaultSprite;
                            });
                    }
                    else
                    {
                        spriteRenderer.sprite = sprite;
                    }
                });
            }
        }
    }
}