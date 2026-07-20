using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace AirSticker.Runtime.Logic
{
    /// <summary>
    /// 贴花投影启动器
    /// </summary>
    /// <remarks>
    /// 贴花投影器是通过这个类启动的。
    /// 贴花投影器在队列中排队，并在适当的时间启动。
    /// </remarks>
    public sealed class DecalProjectorLauncher
    {
        private class LaunchRequest
        {
            public LaunchRequest(AirStickerProjector projector, UnityAction onLaunch)
            {
                Projector = projector;
                OnLaunch = onLaunch;
            }

            public AirStickerProjector Projector { get; }
            public UnityAction OnLaunch { get; }
        }
        
        private readonly Queue<LaunchRequest> _launchRequestQueues = new();
        private LaunchRequest _currentRequest;

        public void Dispose()
        {
            _launchRequestQueues.Clear();
        }
        
        public void Update()
        {
            if (!IsCurrentRequestFinished())
            {
                return; // 当前请求仍在运行，因此返回。
            }

            ProcessNextRequest();
        }

        /// <summary>
        /// 将启动请求排队到队列中。
        /// </summary>
        public void Request(AirStickerProjector projector, UnityAction onLaunch)
        {
            _launchRequestQueues.Enqueue(new LaunchRequest(projector, onLaunch));
        }

        private bool IsCurrentRequestFinished()
        {
            return _currentRequest == null // 空请求
                   || !_currentRequest.Projector // 引发请求的投影仪已死
                   || _currentRequest.Projector.NowState == AirStickerProjector.EState.LaunchingCompleted; // 启动已完成
        }

        private void ProcessNextRequest()
        {
            while (_launchRequestQueues.Count > 0)
            {
                _currentRequest = _launchRequestQueues.Peek();
                _launchRequestQueues.Dequeue();

                // 请求已经失效，所以跳过了
                if (!_currentRequest.Projector)
                {
                    continue;
                }
                
                _currentRequest.OnLaunch?.Invoke();
                break;
            }
        }

        public int GetWaitingRequestCount()
        {
            return _launchRequestQueues.Count;
        }
        
    }
}
