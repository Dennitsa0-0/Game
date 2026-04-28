using ChronoDrop.Core;
using UnityEngine;

namespace ChronoDrop.Replay
{
    public readonly struct ReplayShareRequestedEvent
    {
        public readonly ReplayClipDescriptor Clip;
        public ReplayShareRequestedEvent(ReplayClipDescriptor clip) { Clip = clip; }
    }

    public sealed class ReplayShareController : MonoBehaviour
    {
        [SerializeField] private string creatorId = "@your-id";

        private ReplayClipDescriptor _lastClip;
        private bool _hasClip;

        private void OnEnable()
        {
            EventBus.Subscribe<ReplayClipReadyEvent>(OnReplayClipReady);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ReplayClipReadyEvent>(OnReplayClipReady);
        }

        public void ShareLastReplay()
        {
            if (!_hasClip)
                return;

            EventBus.Raise(new ReplayShareRequestedEvent(_lastClip));
        }

        public string BuildOverlayText(ReplayClipDescriptor clip)
        {
            return $"{clip.Watermark} {creatorId} | {clip.FinalDepthMeters:0}m | {clip.FilterId}";
        }

        private void OnReplayClipReady(ReplayClipReadyEvent evt)
        {
            _lastClip = evt.Clip;
            _hasClip = true;
        }
    }
}
