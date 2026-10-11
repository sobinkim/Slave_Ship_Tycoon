using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI
{
    // Shared by upgrade rows and cargo loading. Pointer movement cancels a hold so a scroll does not purchase repeatedly.
    internal sealed class ToolkitHoldRepeatInput : IDisposable
    {
        private readonly Button button;
        private readonly Clickable originalClickable;
        private readonly Func<bool> trigger;
        private readonly bool triggerOnPress;
        private readonly long delayMilliseconds;
        private readonly long intervalMilliseconds;
        private readonly float dragCancelDistance;
        private IVisualElementScheduledItem repeat;
        private int pointerId = -1;
        private Vector2 pressPosition;
        private bool hasRepeated;

        public ToolkitHoldRepeatInput(Button button, Func<bool> trigger, bool triggerOnPress,
            float delaySeconds, float intervalSeconds, float dragCancelDistance)
        {
            this.button = button;
            originalClickable = button.clickable;
            button.clickable = null;
            this.trigger = trigger;
            this.triggerOnPress = triggerOnPress;
            this.dragCancelDistance = dragCancelDistance;
            delayMilliseconds = (long)(delaySeconds * 1000f);
            intervalMilliseconds = Math.Max(1, (long)(intervalSeconds * 1000f));
            button.RegisterCallback<PointerDownEvent>(OnDown);
            button.RegisterCallback<PointerUpEvent>(OnUp);
            button.RegisterCallback<PointerMoveEvent>(OnMove);
            button.RegisterCallback<PointerLeaveEvent>(OnLeave);
            button.RegisterCallback<PointerCancelEvent>(OnCancel);
            button.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            button.RegisterCallback<NavigationSubmitEvent>(OnSubmit);
        }

        private void OnDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !button.enabledInHierarchy) return;
            Cancel();
            pointerId = evt.pointerId;
            pressPosition = evt.position;
            hasRepeated = false;
            if (triggerOnPress && !trigger()) { Cancel(); return; }
            repeat = button.schedule.Execute(() =>
            {
                hasRepeated = true;
                if (!button.enabledInHierarchy || !trigger()) Cancel();
            }).Every(intervalMilliseconds).StartingIn(delayMilliseconds);
        }

        private void OnUp(PointerUpEvent evt)
        {
            if (pointerId != evt.pointerId) return;
            bool shouldTrigger = !triggerOnPress && !hasRepeated && button.enabledInHierarchy;
            Cancel();
            if (shouldTrigger) trigger();
        }

        private void OnMove(PointerMoveEvent evt)
        {
            if (pointerId == evt.pointerId && Vector2.Distance(pressPosition, evt.position) > dragCancelDistance) Cancel();
        }
        private void OnLeave(PointerLeaveEvent evt) { if (pointerId == evt.pointerId) Cancel(); }
        private void OnCancel(PointerCancelEvent evt) => Cancel();
        private void OnDetach(DetachFromPanelEvent evt) => Cancel();
        private void OnSubmit(NavigationSubmitEvent evt)
        {
            if (button.enabledInHierarchy) trigger();
            evt.StopPropagation();
        }
        public void Cancel() { pointerId = -1; repeat?.Pause(); repeat = null; }
        public void Dispose()
        {
            Cancel();
            button.UnregisterCallback<PointerDownEvent>(OnDown);
            button.UnregisterCallback<PointerUpEvent>(OnUp);
            button.UnregisterCallback<PointerMoveEvent>(OnMove);
            button.UnregisterCallback<PointerLeaveEvent>(OnLeave);
            button.UnregisterCallback<PointerCancelEvent>(OnCancel);
            button.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            button.UnregisterCallback<NavigationSubmitEvent>(OnSubmit);
            button.clickable = originalClickable;
        }
    }
}