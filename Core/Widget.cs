using System;
using System.Collections;
using UnityEngine;

namespace wLib.UIStack
{
    public class Widget : MonoBehaviour, IWidget
    {
        [SerializeField]
        private UILayer _layer = UILayer.Window;

        public int Id { get; private set; }

        public string Path { get; private set; }

        public IWidgetController Controller { get; set; }

        public virtual UILayer Layer => _layer;

        protected IUIManager UIManager { get; private set; }

        public void SetManagerInfo(int id, string path, IUIManager manager)
        {
            Id = id;
            Path = path;
            UIManager = manager;
        }

        #region Events

        public event Action<Widget> OnShowEvent;

        public event Action<Widget> OnHideEvent;

        public event Action<Widget> OnFreezeEvent;

        public event Action<Widget> OnResumeEvent;

        public event Action<Widget> OnDestroyEvent;

        #endregion

        public virtual IEnumerator OnShow()
        {
            yield break;
        }

        public virtual IEnumerator OnHide()
        {
            yield break;
        }

        public virtual IEnumerator OnResume()
        {
            yield break;
        }

        public virtual IEnumerator OnFreeze()
        {
            yield break;
        }

        public void TriggerOnShowEvent()
        {
            OnShowEvent?.Invoke(this);
        }

        public void TriggerOnHideEvent()
        {
            OnHideEvent?.Invoke(this);
        }

        public void TriggerOnFreezeEvent()
        {
            OnFreezeEvent?.Invoke(this);
        }

        public void TriggerOnResumeEvent()
        {
            OnResumeEvent?.Invoke(this);
        }

        public void DestroyWidget()
        {
            OnDestroyEvent?.Invoke(this);
        }

        public virtual void OnDestroy()
        {
            DestroyWidget();
        }
    }
}