using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Gui.Dialog;
using Guilds.Models;
using Guilds.Models.Messages;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UWP;

namespace uGUI.Carousel
{
    public interface ICarouselItemFactory
    {
        IReinitable CreateCarouselItem(IData data);
        void DisposeCarouselItem(IReinitable item);
        void OnCarouselItemUpdate(IReinitable item);
    };

    public interface IReinitable: IDisposable
    {
        IData Data { get; set; }
        
    };

    //объект может получать клик через блокер
    public interface IThrowClickable
    {
        void OnClick();
    };

    public interface IData : IComparable
    {
        Id<Message> Id { get; set; }
    };

    public interface IAnimatedMessage
    {
        void ResetAnimation();
        void StartShowAnimation();
        void StartHideAnimation();
    };

    /// <summary> 
    /// Универсальная динамическая карусель для uGUI. 
    /// Элементы могут быть любыми и разного размера.
    /// Пока только вертикальный скролл;
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class CarouselScrollListWidget : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Tooltip("Фактичесое максимальное количество элементов в списке. Должно быть больше чем видимых ")]
        [SerializeField] private int maxMessagesInPackage = 12;

        [Tooltip("Дельта границы для начала подгрузки; 0-0.5")]
        [SerializeField] private float scrollMinValue = 0.06f;

        [Tooltip("Расстояние между элементами")]
        [SerializeField] private float spacingItems = 10f;

        [Tooltip("количество подгружаемых за раз элементов")]
        [SerializeField] private int countScrollItemUpload = 3;

        public List<IData> DataList { get; set; }
        public ICarouselItemFactory Factory { get; set; }
        public GameObject ScrollingPanel { get { return scroller.content.gameObject; } }
        public event Action<List<IData>> onVisibleElementsChanged = delegate {};

        private ScrollRect scroller;
        private int indexStarPack;

        private List<IData> visibleDataList
        {
            get { return _visibleDataList; }
            set
            {
                _visibleDataList = value;
                onVisibleElementsChanged(_visibleDataList);
            }
        }

        private List<IData> _visibleDataList = new List<IData>();
        private List<IReinitable> _itemViews = new List<IReinitable>();
        private bool inEndList { get { return indexStarPack >= DataList.Count - maxMessagesInPackage; } }
        private bool inStartList { get { return indexStarPack == 0; } }
        private PointerEventData dragEventData;
        private bool isDragging = false;
        private RectTransform content;


        public void Awake()
        {
            _itemViews = new List<IReinitable>();
            visibleDataList = new List<IData>();
            DataList = new List<IData>();
            scroller = GetComponent<ScrollRect>();
            scroller.onValueChanged.AddListener(OnDrag);
            spacingItems = spacingItems * 0.5f;
            content = scroller.content;
        }

        public void GotoFirstPackMessage()
        {
            var count = Mathf.Min(maxMessagesInPackage, DataList.Count);
            visibleDataList = DataList.GetRange(0, count);
            RebuildItems();
        }

        /// <summary> Перейти в конец списка </summary>
        public void GotoLastPackMessage()
        {
            indexStarPack = Mathf.Max(DataList.Count - maxMessagesInPackage, 0);
            var count = Mathf.Min(maxMessagesInPackage, DataList.Count);
            visibleDataList = DataList.GetRange(indexStarPack, count);
            RebuildItems();
            scroller.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
        }

        /// <summary> Добавился новый итем в конец DataList</summary>
        public void AddItem()
        {
            if (visibleDataList.Count < maxMessagesInPackage || DataList.Count < indexStarPack + maxMessagesInPackage + 1 || content.childCount==0 || DataList.Count==0)
            {
                GotoLastPackMessage();
                return;
            }

            var view = content.GetChild(0) as RectTransform;
            view.SetAsLastSibling();
            var item = _itemViews[0];
            _itemViews.Remove(item);
            _itemViews.Add(item);
            indexStarPack = Mathf.Min(indexStarPack + 1, DataList.Count - 1);
            visibleDataList = DataList.GetRange(indexStarPack, maxMessagesInPackage);

            RebuildItems();
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            dragEventData = eventData;
            isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private void OnDrag(Vector2 arg0)
        {
            OnScroll(scroller.normalizedPosition.y);
        }

        private void OnScroll(float value)
        {
            if ((value >= scrollMinValue && value <= 1f - scrollMinValue)
             || (value > 1f - scrollMinValue && inStartList)
             || (value < scrollMinValue && inEndList))
                return;

            scroller.onValueChanged.RemoveAllListeners();

            if (value > 1f - scrollMinValue)
            {
                MoveUp(countScrollItemUpload);
            }
            else if (value < scrollMinValue)
            {
                MoveDown(countScrollItemUpload);
            }
            RebuildItems();

            if (isDragging)
            {
                //грязный хак. Симулируем новый драг
                //https://bitbucket.org/Unity-Technologies/ui/src/0155c39e05ca5d7dcc97d9974256ef83bc122586/UnityEngine.UI/UI/Core/ScrollRect.cs?at=5.2&fileviewer=file-view-default
                scroller.OnBeginDrag(dragEventData);
            }
            scroller.onValueChanged.AddListener(OnDrag);
        }

        private void RebuildItems()
        {
            if (Factory == null)
                return;

            int oldN = _itemViews.Count;
            int newN = Mathf.Min(visibleDataList.Count, DataList.Count);
            int minN = Mathf.Min(oldN, newN);
            
            for (int i = oldN-1; i>=minN; i--)
            {
                Factory.DisposeCarouselItem(_itemViews[i]);
                _itemViews.RemoveAt(i);
            }

            for (int i = minN; i < newN; i++)
            {
                var element = Factory.CreateCarouselItem(visibleDataList[i]);
                _itemViews.Add(element);
            }

            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (!CompareDataType(_itemViews[i].Data, visibleDataList[i]))
                {
                    Factory.DisposeCarouselItem(_itemViews[i]);
                    _itemViews[i] = Factory.CreateCarouselItem(visibleDataList[i]);
                }
                else
                {
                    _itemViews[i].Data = visibleDataList[i];
                    Factory.OnCarouselItemUpdate(_itemViews[i]);
                }
                var rectTransform = ( _itemViews[i] as MonoBehaviour ).transform as RectTransform;
                rectTransform.SetSiblingIndex(i);
            }

            onVisibleElementsChanged(_visibleDataList);
        }

        private bool CompareDataType(IData a, IData b)
        {
            var typeA = a.GetType().GetAttribute<TypeMapAttribute>().Type;
            var typeB = b.GetType().GetAttribute<TypeMapAttribute>().Type;
            return typeA == typeB;
        }

        private void MoveUp(int count = 1)
        {
            var countChild = content.childCount;
            for (int i = 0; i < count; i++)
            {
                if (inStartList)
                    return;
                
                var view = content.GetChild(countChild - 1) as RectTransform;
                var size = view.sizeDelta.y + spacingItems;
                view.SetAsFirstSibling();
                var pos = content.anchoredPosition;
                content.anchoredPosition = new Vector2(pos.x, pos.y + size);

                var data = _itemViews.Last();
                _itemViews.Remove(data);
                _itemViews.Insert(0, data);

                indexStarPack = Mathf.Max(indexStarPack - 1, 0);
                visibleDataList = DataList.GetRange(indexStarPack, maxMessagesInPackage);
            }
        }

        private void MoveDown(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (inEndList)
                    return;

                var view = content.GetChild(0) as RectTransform;
                var size = -1*( view.sizeDelta.y + spacingItems );
                view.SetAsLastSibling();
                var pos = content.anchoredPosition;
                content.anchoredPosition = new Vector2(pos.x, pos.y + size);

                var item = _itemViews[0];
                _itemViews.Remove(item);
                _itemViews.Add(item);
                indexStarPack = Mathf.Min(indexStarPack + 1, DataList.Count - 1);
                visibleDataList = DataList.GetRange(indexStarPack, maxMessagesInPackage);
            }
        }

        
    };
}
