using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace XKAsset
{
	public class BundleAssetMgrInitOp : AssetMgrInitOpBase
	{
		protected override void OnInitLocator(AssetManager mgr)
		{
			string providerId = typeof(BytesAssetProvider).FullName;
			string cfgName = "AssetConfig.csv";

			var location = new ResourceLocationBase(providerId, cfgName,null, null);
			var handle = mgr.ProvideResource<byte[]>(location, typeof(byte[]));
			handle.WaitForCompletion();
			//var tag = new ProfilerTool();
			handle.Completed += (obj) =>
			{
				if (obj == null)
				{
					Debug.LogError("[资源加载] 缺少资源配置文件");
					mgr.HasInited = true;
					return;
				}

				var buildDataInfo = new BuildAssetData();
				buildDataInfo.DeSerialize(obj);
				//tag.TimeTag("BundleAssetMgrInitOp1");
				mgr.AddLoactor(CreateResourceLocator(buildDataInfo));
				mgr.HasInited = true;
				//tag.TimeTag("BundleAssetMgrInitOp2");
				mgr.Release(handle);
			};
		}

		protected override void OnInitProvider(AssetManager mgr)
		{
			mgr.AddProvider(new AssetBundleProvider());
			mgr.AddProvider(new BundleAssetProvider());
			mgr.AddProvider(new AtlasAssetProvider());
		}

		private IResourceLocator CreateResourceLocator(BuildAssetData data)
		{
			ResourceMapLocator locator = new ResourceMapLocator();
			foreach (var info in data.assetData)
			{
				var deps = CreateDependencies(info.Value.deps, locator, info.Value.pkgType);
				string providerId = GetProviderId(info.Value.pkgType, info.Value.providerType);

				if (!locator.TryGetValue(info.Key, out IResourceLocation loc))
				{
					string path = info.Value.pkgType == AssetPkgType.PT_BUNDLE ? info.Key : info.Value.deps[0];	//如果是流加载，取转换后的资源
					loc = new ResourceLocationBase(providerId, path, typeof(object), deps);
					locator.Add(info.Key, loc);
				}
				else
				{
					var locBase = (ResourceLocationBase) loc;
					locBase.SetDeps(deps);
					locBase.SetType(typeof(object));
					locBase.SetProviderId(providerId);
				}
			}
			return locator;
		}
		
		/// <summary>
		/// 创建依赖项
		/// </summary>
		/// <param name="deps">依赖项</param>
		/// <param name="locator">定位器</param>
		/// <returns></returns>
		private IList<IResourceLocation> CreateDependencies(List<string> deps, ResourceMapLocator locator, AssetPkgType type)
		{
			if (type == AssetPkgType.PT_STREAM)
				return null;	//流加载是直接资源，不依赖
			if (deps == null || deps.Count == 0)
			{
				return null;
			}
			IList<IResourceLocation> listLocs = new List<IResourceLocation>();
			foreach (var path in deps)
			{
				if (!locator.TryGetValue(path, out IResourceLocation loc))
				{
					loc = new ResourceLocationBase(typeof(AssetBundleProvider).FullName, path, typeof(AssetBundle), null);
					locator.Add(path, loc);
				}
				listLocs.Add(loc);
			}

			return listLocs;
		}

		private string GetProviderId(AssetPkgType pkgType, ProviderType type)
		{
			if (pkgType == AssetPkgType.PT_STREAM)
			{
				return typeof(BytesAssetProvider).FullName;
			}
			else
			{
				switch (type)
				{
					case ProviderType.BUNDLE:
						return typeof(AssetBundleProvider).FullName;
					case ProviderType.ASSET:
						return typeof(BundleAssetProvider).FullName;
					case ProviderType.ATLAS:
						return typeof(AtlasAssetProvider).FullName;
					default:
						return typeof(AssetBundleProvider).FullName;
				}
			}
		}
	}
}