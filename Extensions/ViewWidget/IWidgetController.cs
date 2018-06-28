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

    public abstract class WidgetController<TView> : IWidgetController<TView> where TView : ViewWidget
    {
        public TView Widget { get; private set; }

        public virtual void SetWidget(object widget)
        {
            Widget = widget as TView;
        }

        public virtual void OnShow(UIMessage message) { }

        public virtual void OnHide() { }

        public virtual void OnResume() { }

        public virtual void OnFreeze() { }
    }
}