using System;
using System.Collections;

namespace wLib.UIStack
{
    public abstract class ViewWidget : AddressableWidget
    {
        public event Action<UIMessage> OnShowAction;
        public event Action OnHideAction;
        public event Action OnFreezeAction;
        public event Action OnResumeAction;
        public event Action OnUpdateAction;
        public event Action OnDestroyAction;

        public override IEnumerator OnShow(UIMessage message)
        {
            OnShowAction?.Invoke(message);
            yield return null;
        }

        public override IEnumerator OnHide()
        {
            OnHideAction?.Invoke();
            yield return null;
        }

        public override IEnumerator OnFreeze()
        {
            OnFreezeAction?.Invoke();
            yield return null;
        }

        public override IEnumerator OnResume()
        {
            OnResumeAction?.Invoke();
            yield return null;
        }

        protected virtual void Update()
        {
            OnUpdateAction?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            OnDestroyAction?.Invoke();
        }
    }
}