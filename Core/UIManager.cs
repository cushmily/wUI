using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using wLib.Injection;

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
        private static readonly Stack<Widget> StackedWindows = new Stack<Widget>();
        private static readonly List<int> WindowsInDisplay = new List<int>();
        private static readonly List<int> Popups = new List<int>();
        private static readonly List<int> Fixes = new List<int>();

        private static readonly Dictionary<int, Widget> WidgetLookup = new Dictionary<int, Widget>();
        private static readonly Dictionary<UILayer, GameObject> LayerLookup = new Dictionary<UILayer, GameObject>();
        private static readonly Dictionary<Type, IWidgetFactory> FactoryLookup = new Dictionary<Type, IWidgetFactory>();

        private static readonly Dictionary<Type, Stack<Widget>> PoolingWidgets = new Dictionary<Type, Stack<Widget>>();

        private static DiContainer _container;

        public static UIManager BuildHierarchy(DiContainer container, bool landscapeOrientation = true,
            Vector2? refResolution = null)
        {
            _container = container;

            CollectFactories();

            var manager = new GameObject("UiManager").AddComponent<UIManager>();

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

                var layerCanvasScaler = layerObj.AddComponent<CanvasScaler>();
                layerCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                layerCanvasScaler.referenceResolution = refResolution ?? (landscapeOrientation
                                                            ? new Vector2(1920, 1080)
                                                            : new Vector2(1080, 1920));
                layerCanvasScaler.matchWidthOrHeight = landscapeOrientation ? 1 : 0;

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

        #region Push

        public void Push(string widgetName)
        {
            Push<Widget>(widgetName, UIMessage.Empty, null);
        }

        public void Push(string widgetName, Action<int> onCreated)
        {
            Push<Widget>(widgetName, UIMessage.Empty, onCreated);
        }

        public void Push(string widgetName, UIMessage message)
        {
            Push<Widget>(widgetName, message, null);
        }

        public void Push(string widgetName, UIMessage message, Action<int> onCreated)
        {
            Push<Widget>(widgetName, message, onCreated);
        }

        public void Push<TWidget>() where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, null);
        }

        public void Push<TWidget>(Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, onCreated);
        }

        public void Push<TWidget>(UIMessage message) where TWidget : Widget
        {
            Push<TWidget>(null, message, null);
        }

        public void Push<TWidget>(UIMessage message, Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(null, UIMessage.Empty, null);
        }

        public void Push<TWidget>(string widgetName) where TWidget : Widget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, null);
        }

        public void Push<TWidget>(string widgetName, Action<int> onCreated) where TWidget : Widget
        {
            Push<TWidget>(widgetName, UIMessage.Empty, onCreated);
        }

        public void Push<TWidget>(string widgetName, UIMessage message) where TWidget : Widget
        {
            Push<TWidget>(widgetName, message, null);
        }

        public void Push<TWidget>(string widgetName, UIMessage message, Action<int> onCreated)
            where TWidget : Widget
        {
            var id = GetId();
            GetInstance<TWidget>(widgetName, id, instance =>
            {
                var parent = LayerLookup[instance.Layer];
                instance.transform.SetParent(parent.transform, false);
//                instance.transform.SetAsFirstSibling();

                if (instance.Layer == UILayer.Popup) { Popups.Add(instance.Id); }

                if (instance.Layer == UILayer.Fixed) { Fixes.Add(instance.Id); }

                if (StackedWindows.Count > 0)
                {
                    var prevWidget = StackedWindows.Peek();
                    RunCoroutine(prevWidget.OnFreeze(), () =>
                    {
                        prevWidget.TriggerOnFreezeEvent();

                        // Window will overlay previous windows.
                        if (instance.Layer == UILayer.Window && WindowsInDisplay.Contains(prevWidget.Id))
                        {
                            WindowsInDisplay.Remove(prevWidget.Id);
                        }
                    });
                    RunCoroutine(instance.OnShow(message), () =>
                    {
                        instance.TriggerOnShowEvent();
                        onCreated?.Invoke(id);

                        StackedWindows.Push(instance);
                        WidgetLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
                else
                {
                    RunCoroutine(instance.OnShow(message), () =>
                    {
                        instance.TriggerOnShowEvent();
                        onCreated?.Invoke(id);

                        StackedWindows.Push(instance);
                        WidgetLookup.Add(id, instance);
                        WindowsInDisplay.Add(id);
                    });
                }
            });
        }

        #endregion

        #region Pop

        public void Pop(bool recycle = false)
        {
            Pop(null, recycle);
        }

        public void Pop(Action onDone, bool recycle = false)
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
                    if (recycle) { MoveToHidden(current); }
                    else
                    {
                        WidgetLookup.Remove(current.Id);
                        Destroy(current.gameObject);
                    }

                    current.TriggerOnHideEvent();
                    onDone?.Invoke();

                    // resume previous window
                    var resumeWindow = StackedWindows.Peek();
                    RunCoroutine(resumeWindow.OnResume(), () => { resumeWindow.TriggerOnResumeEvent(); });
                });
            }
            else
            {
                RunCoroutine(current.OnHide(), () =>
                {
                    if (recycle) { MoveToHidden(current); }
                    else
                    {
                        WidgetLookup.Remove(current.Id);
                        Destroy(current.gameObject);
                    }

                    current.TriggerOnHideEvent();
                    onDone?.Invoke();
                });
            }
        }

        #endregion

        #region Clear

        public void ClearPopups()
        {
            foreach (var popup in Popups) { Close(popup); }

            Popups.Clear();
        }

        public void ClearFixes()
        {
            foreach (var fix in Fixes) { Close(fix); }

            Fixes.Clear();
        }

        public void ClearWindows()
        {
            while (StackedWindows.Count > 0)
            {
                var window = StackedWindows.Pop();
                Close(window.Id);
            }
        }

        public void ClearAll()
        {
            ClearPopups();
            ClearFixes();
            ClearWindows();
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
                    onClosed?.Invoke();

                    if (WindowsInDisplay.Contains(widgetId)) { WindowsInDisplay.Remove(widgetId); }
                });
            }
        }

        #endregion

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
                    if (att == null) { continue; }

//                        Debug.Log("Collect " + att.WidgetType);
                    var factoryInstance = Activator.CreateInstance(factoryType) as IWidgetFactory;
                    if (factoryInstance == null) { continue; }

                    _container.Inject(factoryInstance);
                    factoryInstance.SetupFactory();
                    RegisterFactory(att.WidgetType, factoryInstance);
                }
            }
        }

        private void GetInstance<T>(string widgetName, int assignedId, Action<T> onCreated) where T : Widget
        {
            if (PoolingWidgets.ContainsKey(typeof(T)))
            {
                var pool = PoolingWidgets[typeof(T)];
                if (pool.Count > 0)
                {
                    var instance = pool.Pop();

                    onCreated.Invoke(instance as T);
                    return;
                }
            }

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
                        //Too annoying.
//                        Debug.LogWarningFormat(
//                            "Widget factory not found for type: {0}, fallback to factory: {1}.",
//                            typeof(T), factory);
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
                genericFactory?.CreateInstance(this, widgetName, assignedId, onCreated);
            }
            else
            {
                factory.CreateInstance(this, widgetName, assignedId,
                    widgetCreated =>
                    {
                        var genericWidget = widgetCreated as T;
                        if (genericWidget == null)
                        {
                            Debug.LogWarningFormat("Can not convert [{0}] to type: {1}", widgetCreated, typeof(T));
                        }

                        onCreated.Invoke(genericWidget);
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
            onDone?.Invoke();
        }

        private int GetId()
        {
            return _componentId++;
        }

        public Widget Get(int id)
        {
            Widget targetComp;
            if (!WidgetLookup.TryGetValue(id, out targetComp))
            {
                Debug.LogWarningFormat("Can't load widget of id: {0}.", id);
            }

            return targetComp;
        }

        public TUiComponent Get<TUiComponent>(int id) where TUiComponent : Widget
        {
            Widget targetComp;
            if (!WidgetLookup.TryGetValue(id, out targetComp))
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

        private void MoveToHidden(Widget toHide)
        {
            var hiddenLayer = LayerLookup[UILayer.UIHidden];
            toHide.transform.SetParent(hiddenLayer.transform);

            var type = toHide.GetType();
            if (PoolingWidgets.ContainsKey(type)) { PoolingWidgets[type].Push(toHide); }
            else
            {
                var newStack = new Stack<Widget>();
                newStack.Push(toHide);
                PoolingWidgets.Add(type, newStack);
            }
        }

        #endregion
    }
}