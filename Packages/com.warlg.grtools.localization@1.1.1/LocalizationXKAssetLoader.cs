using System;
using UnityEngine;
using XKAsset;
using Object = UnityEngine.Object;

namespace GRTools.Localization
{
    public class LocalizationXKAssetLoader : LocalizationLoader
    {
        public string ManifestPath;


        public LocalizationXKAssetLoader()
        {
            ManifestPath = "assetfiles/localization/LocalizationManifest.asset";
        }

        public override void LoadManifestAsync(Action<bool, LocalizationInfo[]> completed)
        {
            if (completed != null)
            {
                AsyncOperationHandle<LocalizationManifest> request =
                    AssetManager.Instance.LoadAssetAsync<LocalizationManifest>(ManifestPath);
                request.Completed += operation =>
                {
                    if (operation != null)
                    {
                        LocalizationInfo[] infoList = operation.InfoList;
                        if (infoList != null)
                        {
                            LocalizationInfo[] newInfoList = new LocalizationInfo[infoList.Length];
                            for (int i = 0; i < infoList.Length; i++)
                            {
                                newInfoList[i] = infoList[i];
                            }
                            completed.Invoke(true, newInfoList);
                        }
                        else
                        {
                            completed(false, new LocalizationInfo[0]);
                        }

                        // AssetManager.Instance.Release(request);
                    }
                    else
                    {
                        completed.Invoke(false, null);
                    }
                };
            }
        }

        private void RequestCompleted(LocalizationManifest manifest)
        {
        }

        public override void LoadLocalizationTextAsset(LocalizationInfo info, Action<Object> completed)
        {
            //LocalizationManager.Singleton.CurrentLocalizationInfo
            LoadAsset(info, completed);
        }

        private void LoadAsset<TAsset>(LocalizationInfo info, Action<TAsset> completed) where TAsset : Object
        {
            var request = AssetManager.Instance.LoadAssetAsync<TAsset>(info.TextAssetPath);

            void RequestOnCompleted(Object operation)
            {
                if (operation != null)
                {
                    request.Completed -= RequestOnCompleted;
                    completed?.Invoke(operation as TAsset);
                }
                LanguageMasterManager.Singleton.HasInited = true;
                Debug.Log("语言文件列表加载完成");
            }

            request.Completed += RequestOnCompleted;
        }

        public override void LoadAssetAsync<TAsset>(LocalizationInfo info, string assetName, Action<TAsset> completed)
        {
            var request = AssetManager.Instance.LoadAssetAsync<TAsset>(assetName);
            request.Completed += operation =>
            {
                if (operation != null)
                {
                    completed.Invoke(operation);
                }
            };
        }
    }
}