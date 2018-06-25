using System.Collections;
using UnityEngine;

namespace wLib.UIStack
{
    public class BaseWidget : MonoBehaviour
    {
        public int Id { get; private set; }

        protected IUIManager UIManager { get; private set; }

        public void SetManagerInfo(int id, IUIManager manager)
        {
            Id = id;
            UIManager = manager;
        }

        public virtual UILayer Layer => UILayer.Window;

        public virtual IEnumerator OnShow(UIMessage message)
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
    }
}