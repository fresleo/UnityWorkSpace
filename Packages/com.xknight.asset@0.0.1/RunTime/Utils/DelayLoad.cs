#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace XKAsset
{
	internal class DelayLoad : MonoBehaviour
	{
		private static DelayLoad _ins;
		private static DelayLoad Ins
		{
			get
			{
				if (_ins == null)
					_ins = CreateDelayLoad();
				return _ins;
			}
		}

		private Queue<DelayActionData> _listAction;

		private void Awake()
		{
			_listAction = new Queue<DelayActionData>();
		}

		[Conditional("UNITY_EDITOR")]
		public static void Add(Action ac, float delayTime)
		{
			Ins.AddInternal(ac, delayTime);
		}

		
		private void AddInternal(Action ac, float delayTime)
		{
			_listAction.Enqueue(new DelayActionData(ac, delayTime));
		}

		private void Update()
		{
			UpdateInternal();
		}

		private void UpdateInternal()
		{
			if (_listAction.Count <= 0)
				return;
			while (_listAction.TryPeek(out var delayAction))
			{
				if (delayAction.CanExecute())
				{
					delayAction.callBack?.Invoke();
					_listAction.Dequeue(); //真正弹出
				}
				else
					break;	//默认加载时长，当有一个不满足时后续肯定都不满足
			}
		}

		public void Clear()
		{
			_listAction.Clear();
		}

		private static DelayLoad CreateDelayLoad()
		{
			var delayLoad = FindObjectOfType<DelayLoad>();
			if (delayLoad == null)
			{
				var obj = new GameObject("DelayLoad_OnlyEditor");
				if (Application.isPlaying)
				{
					DontDestroyOnLoad(obj);
					obj.hideFlags = HideFlags.DontSave;
				}
				else
				{
					obj.hideFlags = HideFlags.HideAndDontSave;
				}

				delayLoad = obj.AddComponent<DelayLoad>();
			}

			return delayLoad;
		}
	}

	internal struct DelayActionData
	{
		public Action callBack;
		private readonly float _delayTime;
		private float _startTime;

		public DelayActionData(Action ac, float time)
		{
			callBack = ac;
			_delayTime = time;
			_startTime = Time.realtimeSinceStartup;
		}

		public bool CanExecute()
		{
			var lastTime = Time.realtimeSinceStartup - _startTime;
			return _delayTime <= lastTime;
		}
	}
}
#endif