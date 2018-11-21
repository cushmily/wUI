using System;

namespace wLib.UIStack
{
    public interface IWidgetController : IDisposable
    {
        void SetControllerInfo(Widget widget, IUIManager manager, UIMessage message);
        void Initialise();
        void OnDestroy();
    }
}