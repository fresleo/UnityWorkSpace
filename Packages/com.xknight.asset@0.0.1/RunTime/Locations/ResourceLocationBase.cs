using System;
using System.Collections.Generic;

namespace XKAsset
{
	internal class ResourceLocationBase : IResourceLocation
	{
		private string _resourceName;
		private string _absPath;
		private string _absUrl;
		
		public string ProviderId { get; private set; }
		public string RelativePath { get; private set; }

		public string AbsPath
		{
			get
			{
				if (string.IsNullOrEmpty(_absPath))
					_absPath = AssetLoadGlobalConfig.GetAssetPath(RelativePath);
				return _absPath;
			}
		}

		public string AbsUrl
		{
			get
			{
				if (string.IsNullOrEmpty(_absUrl))
					_absUrl = AssetLoadGlobalConfig.GetAssetUrl(RelativePath);
				return _absUrl;
			}
		}
		public IList<IResourceLocation> Dependencies { get; private set; }
		public Type ResourceType { get; private set; }
		public bool HasDeps => Dependencies != null && Dependencies.Count > 0;
		public int HashCode { get; private set; }
		public int DepsHashCode { get; private set; }

		

		public string ResourceName
		{
			get
			{
				if(string.IsNullOrEmpty(_resourceName))
					_resourceName = string.Intern(AssetLoadGlobalConfig.GetAssetName(RelativePath));
				return _resourceName;
			}
		}

		public ResourceLocationBase(string providerId, string path, Type t, IList<IResourceLocation> deps)
		{
			ProviderId = string.Intern(providerId);
			SetResourcePath(path);
			
			ResourceType = t == null ? typeof(object) : t;
			if (deps != null)
			{
				Dependencies = new List<IResourceLocation>(deps);
				GenerateDepsHashCode();
			}
				
			HashCode = RelativePath.GetHashCode();
		}

		public void SetProviderId(string id)
		{
			ProviderId = id;
		}

		public void SetDeps(IList<IResourceLocation> locs)
		{
			Dependencies = locs;
		}

		public void SetType(Type t)
		{
			ResourceType = t;
		}

		private void GenerateDepsHashCode()
		{
			DepsHashCode = 17;
			for (int i = 0; i < Dependencies.Count; i++)
			{
				DepsHashCode = DepsHashCode * 31 + Dependencies[i].HashCode.GetHashCode();
			}
		}

		private void SetResourcePath(string strPath)
		{
			RelativePath = string.Intern(strPath);
		}
	}
}