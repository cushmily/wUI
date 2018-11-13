using System;

namespace wLib.UIStack
{
    public interface IWidgetController : IDisposable
    {
        void Initialise();
        void OnDestroy();
    }
}