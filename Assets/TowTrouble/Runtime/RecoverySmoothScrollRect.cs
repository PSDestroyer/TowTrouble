using UnityEngine;
using UnityEngine.EventSystems;

namespace TowTrouble
{
    // Menu card lists. Dragging and the scrollbar retain direct pointer control.
    public sealed class RecoverySmoothScrollRect : UnityEngine.UI.ScrollRect
    {
        public bool ReducedMotion { get; set; }
        public float ScrollSpeed { get; set; }
        private float target;
        private bool moving;

        public void ScrollTo(float offset)
        {
            if (content == null || viewport == null) return;
            target = Mathf.Clamp(offset, 0, Mathf.Max(0, content.rect.height - viewport.rect.height));
            StopMovement();
            moving = !ReducedMotion && Mathf.Abs(target - content.anchoredPosition.y) > .1f;
            if (!moving)
            {
                SetContentAnchoredPosition(new Vector2(content.anchoredPosition.x, target));
                ScrollSpeed = 0;
            }
        }

        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive() || content == null || viewport == null) return;
            float delta = -data.scrollDelta.y;
            if (Mathf.Abs(data.scrollDelta.x) > Mathf.Abs(delta)) delta = data.scrollDelta.x;
            ScrollTo((moving ? target : content.anchoredPosition.y) + delta * scrollSensitivity);
        }

        public override void OnBeginDrag(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) CancelAnimation();
            base.OnBeginDrag(data);
        }

        protected override void SetNormalizedPosition(float value, int axis)
        {
            CancelAnimation();
            base.SetNormalizedPosition(value, axis);
        }

        protected override void LateUpdate()
        {
            if (moving && content != null && viewport != null && Time.unscaledDeltaTime > 0)
            {
                target = Mathf.Clamp(target, 0, Mathf.Max(0, content.rect.height - viewport.rect.height));
                float speed = ScrollSpeed;
                float y = Mathf.SmoothDamp(content.anchoredPosition.y, target, ref speed, .10f,
                    Mathf.Infinity, Time.unscaledDeltaTime);
                ScrollSpeed = speed;
                if (Mathf.Abs(y - target) < .1f) { y = target; CancelAnimation(); }
                StopMovement();
                SetContentAnchoredPosition(new Vector2(content.anchoredPosition.x, y));
            }
            // Native ScrollRect updates the scrollbar/events after our animation step.
            base.LateUpdate();
        }

        private void CancelAnimation() { moving = false; ScrollSpeed = 0; }
        protected override void OnDisable() { CancelAnimation(); base.OnDisable(); }
    }
}
