namespace wLib.UIStack
{
    public interface IWidgetController
    {
        void SetWidget(object widget);

        void OnShow(UIMessage message);

        void OnHide();

        void OnResume();

        void OnFreeze();
    }

    public interface IWidgetController<out TView> : IWidgetController where TView : ViewWidget
    {
        TView Widget { get; }
    }
}