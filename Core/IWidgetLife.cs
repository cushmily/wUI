using System.Collections;

namespace wLib.UIStack
{
    public interface IWidgetLife
    {
        IEnumerator OnShow(UIMessage message);

        IEnumerator OnHide();

        IEnumerator OnResume();

        IEnumerator OnFreeze();
    }
}