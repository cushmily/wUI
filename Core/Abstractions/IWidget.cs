using System.Collections;

namespace wLib.UIStack
{
    public interface IWidget
    {
        IEnumerator OnShow(UIMessage message);

        IEnumerator OnHide();

        IEnumerator OnResume();

        IEnumerator OnFreeze();

        void DestroyWidget();
    }
}