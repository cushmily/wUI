using System;
using System.Collections.Generic;
using System.Linq;

namespace wLib.UIStack
{
    [CustomWidgetFactory(typeof(ViewWidget))]
    public class ViewWidgetFactory : AddressableWidgetFactory, IWidgetFactory<ViewWidget>
    {
        private readonly Dictionary<Type, Type> _controllerRef = new Dictionary<Type, Type>();

        protected readonly Dictionary<int, IViewWidgetController>
            ControllerCaches = new Dictionary<int, IViewWidgetController>();

        public ViewWidgetFactory()
        {
            var controllerTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => !x.IsAbstract && !x.IsInterface && typeof(IViewWidgetController).IsAssignableFrom(x) &&
                            x != typeof(IViewWidgetController)).ToArray();

            foreach (var controllerType in controllerTypes)
            {
                var interfaces = controllerType.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    var intf = interfaces[i];
                    if (!typeof(IViewWidgetController).IsAssignableFrom(intf) || intf == typeof(IViewWidgetController))
                    {
                        continue;
                    }

                    var args = intf.GetGenericArguments();
                    if (args.Length <= 0) { continue; }

                    var targetType = args[0];
                    _controllerRef.Add(targetType, controllerType);
                }
            }
        }

        public override void CreateInstance(IUIManager manager, string widgetName, int assignedId,
            Action<BaseWidget> onCreated)
        {
            CreateInstance(manager, widgetName, assignedId, widget => onCreated.Invoke(widget));
        }

        public virtual object GetInstance(Type instanceType)
        {
            return Activator.CreateInstance(instanceType);
        }

        public virtual void CreateInstance(IUIManager manager, string widgetName, int assignedId,
            Action<ViewWidget> onCreated)
        {
            CreateInstance(manager, widgetName, assignedId, (AddressableWidget widget) =>
            {
                var viewWidget = (ViewWidget) widget;
                viewWidget.SetManagerInfo(assignedId, manager);
                var widgetType = viewWidget.GetType();

                if (_controllerRef.ContainsKey(widgetType))
                {
                    var controllerType = _controllerRef[widgetType];
                    var controller = GetInstance(controllerType) as IViewWidgetController;
                    if (controller != null)
                    {
                        controller.SetWidget(manager, viewWidget, assignedId);
                        viewWidget.OnShowAction += controller.OnShow;
                        viewWidget.OnHideAction += controller.OnHide;
                        viewWidget.OnFreezeAction += controller.OnFreeze;
                        viewWidget.OnResumeAction += controller.OnResume;
                        viewWidget.OnUpdateAction += controller.OnUpdate;
                        viewWidget.OnDestroyAction += () =>
                        {
                            viewWidget.OnShowAction -= controller.OnShow;
                            viewWidget.OnHideAction -= controller.OnHide;
                            viewWidget.OnFreezeAction -= controller.OnFreeze;
                            viewWidget.OnResumeAction -= controller.OnResume;
                            viewWidget.OnUpdateAction -= controller.OnUpdate;
                            controller = null;
                        };

                        ControllerCaches.Add(assignedId, controller);
                    }
                }

                if (onCreated != null) { onCreated.Invoke(viewWidget); }
            });
        }
    }
}