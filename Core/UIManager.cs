using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace wLib.UIStack
{
    public enum UILayer
    {
        UIHidden = -1,
        Background = 0,
        Window = 1,
        Fixed = 2,
        Popup = 3,
        Mask = 4
    }

    public class UIManager : MonoBehaviour, IUIManager
    {
        private static readonly Stack<BaseWidget> StackedWindows = new Stack<BaseWidget>();
        private static readonly List<int> WindowsInDisplay = new List<int>();

        private static readonly Dictionary<int, BaseWidget> ComponentLookup = new Dictionary<int, BaseWidget>();
        private static readonly Dictionary<UILayer, GameObject> LayerLookup = new Dictionary<UILayer, GameObject>();
        private static readonly Dictionary<Type, IWidgetFactory> FactoryLookup = new Dictionary<Type, IWidgetFactory>();

        public static UIManager BuildHirerachy()
        {
            CollectFactories();

            var manager = new GameObject("UiManager").AddComponent<UIManager>();
            DontDestroyOnLoad(manager);

            var uiCam = new GameObject("UiCamera", typeof(Camera)).GetComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            uiCam.orthographic = true;
            uiCam.depth = 10;
            uiCam.transform.SetParent(manager.transform);
            uiCam.transform.localPosition = Vector3.zero;

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                var layerObj = new GameObject(layer.ToString());
                LayerLookup.Add(layer, layerObj);

                var layerCanvas = layerObj.AddComponent<Canvas>();
                layerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                layerCanvas.worldCamera = uiCam;
                layerCanvas.sortingOrder = (int) layer;

                var layerCanvaseScaler = layerObj.AddComponent<CanvasScaler>();
                layerCanvaseScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                layerCanvaseScaler.referenceResolution = new Vector2(1920, 1080);
                layerCanvaseScaler.matchWidthOrHeight = 1;

                var layerRaycaster = layerObj.AddComponent<GraphicRaycaster>();
                layerRaycaster.name = layer.ToString();
                layerRaycaster.transform.SetParent(manager.transform);
                layerRaycaster.transform.localPosition = Vector3.zero;

                if (layer == UILayer.UIHidden)
                {
                    layerObj.layer = LayerMask.NameToLayer("UIHidden");
                    layerRaycaster.enabled = false;
                }
                else { layerObj.layer = LayerMask.NameToLayer("UI"); }
            }

            return manager;
        }

        #region Widget Manager

        private int _componentId;

        public void Push<TWidget>(string widgetName) where TWidget : BaseWidget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, null);
        }

        public void Push<TWidget>(string widgetName, Action<int> onCreated) where TWidget : BaseWidget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, onCreated);
        }

        public void Push<TWidget>(string widgetName, UIMessage message) where TWidget : BaseWidget
        {
            Push<TWidget>(widgetName, message, null);
        }

        public void Push<TWidget>(string widgetName, UIMessage message, Action<int> onCreated)
            where TWidget : BaseWidget
        {
            var id = GetId();
            GetInstance<TWidget>(widgetName, id, instance =>
            {
                var parent = LayerLookup[instance.Layer];
                instance.transform.SetParent(parent.transform, false);
                instance.transform.SetAsFirstSibling();

                if (StackedWindows.Count > 0)
                {
                    var prevWidget = StackedWindows.Peek();
                    RunCoroutine(prevWidget.OnFreeze(), () =>
                    {
                        // Window will overlay previous windows.
                        if (instance.Layer == UILayer.Window && WindowsInDisplay.Contains(prevWidget.Id))
                        {
                            WindowsInDisplay.Remove(prevWidget.Id);
                        }
                    });
                    RunCoroutine(instance.OnShow(message), () =>
                    {
                        if (onCreated != null) { onCreated.Invoke(id); }

                        StackedWindows.Push(instance);
                        ComponentLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
                else
                {
                    RunCoroutine(instance.OnShow(message), () =>
                    {
                        if (onCreated != null) { onCreated.Invoke(id); }

                        StackedWindows.Push(instance);
                        ComponentLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
            });
        }

        public void Pop()
        {
            Pop(null);
        }

        public void Pop(Action onPoped)
        {
            if (StackedWindows.Count < 0)
            {
                Debug.LogWarning("Nothing to pop.");
                return;
            }

            var current = StackedWindows.Pop();

            if (StackedWindows.Count > 0)
            {
                RunCoroutine(current.OnHide(), () =>
                {
                    MoveToHidden(current);
                    if (onPoped != null) { onPoped.Invoke(); }

                    RunCoroutine(StackedWindows.Peek().OnResume(), null);
                });
            }
            else
            {
                RunCoroutine(current.OnHide(), () =>
                {
                    MoveToHidden(current);
                    if (onPoped != null) { onPoped.Invoke(); }
                });
            }
        }

        public void Close(int widgetId)
        {
            Close(widgetId, null);
        }

        public void Close(int widgetId, Action onClosed)
        {
            var targetWidget = Get(widgetId);
            if (targetWidget.Layer != UILayer.Window || !WindowsInDisplay.Contains(widgetId))
            {
                RunCoroutine(targetWidget.OnHide(), () =>
                {
                    MoveToHidden(targetWidget);
                    if (onClosed != null) { onClosed.Invoke(); }

                    if (WindowsInDisplay.Contains(widgetId)) { WindowsInDisplay.Remove(widgetId); }
                });
            }
        }

        #endregion

        #region Helpers

        public static void RegisterFactory(Type type, IWidgetFactory factory)
        {
            if (FactoryLookup.ContainsKey(type))
            {
                Debug.LogErrorFormat("Factory already registered for type: {0}.", type);
            }

            FactoryLookup[type] = factory;
        }

        #endregion

        #region Internal Funtions

        private static void CollectFactories()
        {
            if (FactoryLookup.Count > 0) { return; }

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => typeof(IWidgetFactory).IsAssignableFrom(x));

            foreach (var factoryType in types)
            {
                if (!factoryType.IsAbstract && !factoryType.IsInterface)
                {
                    var atts = factoryType.GetCustomAttributes(typeof(CustomWidgetFactoryAttribute), true);
                    if (atts.Length <= 0) { continue; }

                    var att = atts[0] as CustomWidgetFactoryAttribute;
                    if (att != null)
                    {
                        Debug.Log("Collect " + att.WidgetType);
                        var factoryInstance = Activator.CreateInstance(factoryType) as IWidgetFactory;
                        RegisterFactory(att.WidgetType, factoryInstance);
                    }
                }
            }
        }

        private void GetInstance<T>(string widgetName, int assignedId, Action<T> onCreated) where T : BaseWidget
        {
            var useSpecifiedFactory = false;
            IWidgetFactory factory;
            var resolveType = typeof(T);
            if (!FactoryLookup.TryGetValue(resolveType, out factory))
            {
                while (factory == null && resolveType.BaseType != null)
                {
                    resolveType = resolveType.BaseType;
                    FactoryLookup.TryGetValue(resolveType, out factory);
                    if (factory != null)
                    {
                        Debug.LogWarningFormat(
                            "Widget factory not found for type: {0}, fallback to factory: {1}.",
                            typeof(T), factory);
                    }
                }

                if (factory == null)
                {
                    Debug.LogError("Widget factory not found for type: {0}, no fallback.");
                    return;
                }
            }
            else { useSpecifiedFactory = true; }

            if (useSpecifiedFactory)
            {
                var genericFactory = factory as IWidgetFactory<T>;
                if (genericFactory != null) { genericFactory.CreateInstance(this, widgetName, assignedId, onCreated); }
            }
            else
            {
                factory.CreateInstance(this, widgetName, assignedId,
                    widgetCreated =>
                    {
                        var genericWdiget = widgetCreated as T;
                        if (genericWdiget == null)
                        {
                            Debug.LogWarningFormat("Can not convert [{0}] to type: {1}", widgetCreated, typeof(T));
                        }

                        onCreated.Invoke(genericWdiget);
                    });
            }
        }

        private void RunCoroutine(IEnumerator target, Action onDone)
        {
            StartCoroutine(MonitorCoroutine(target, onDone));
        }

        private IEnumerator MonitorCoroutine(IEnumerator target, Action onDone)
        {
            yield return target;
            if (onDone != null) { onDone.Invoke(); }
        }

        private int GetId()
        {
            return _componentId++;
        }

        public BaseWidget Get(int id)
        {
            BaseWidget targetComp;
            if (!ComponentLookup.TryGetValue(id, out targetComp))
            {
                Debug.LogWarningFormat("Can't load widget of id: {0}.", id);
            }

            return targetComp;
        }

        public TUiComponent Get<TUiComponent>(int id) where TUiComponent : BaseWidget
        {
            BaseWidget targetComp;
            if (!ComponentLookup.TryGetValue(id, out targetComp))
            {
                Debug.LogWarningFormat("Can't load widget of id: {0}.", id);
            }
            else
            {
                var resultComponent = targetComp as TUiComponent;
                if (resultComponent != null) { return resultComponent; }
            }

            return null;
        }

        private void MoveToHidden(BaseWidget baseToHide)
        {
            var hiddenLayer = LayerLookup[UILayer.UIHidden];
            baseToHide.transform.SetParent(hiddenLayer.transform);
        }

        #endregion
    }
}