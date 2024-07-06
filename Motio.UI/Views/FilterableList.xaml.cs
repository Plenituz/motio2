using Motio.NodeCommon.ObjectStoringImpl;
using Motio.PythonRunning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for FilterableList.xaml
    /// </summary>
    public partial class FilterableList : UserControl, INotifyPropertyChanged
    {
        private IEnumerable _oldItems;
        public IEnumerable ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable; }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(FilterableList), null);

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<object> Validated;
        public event Action Cancel;

        public Predicate<object> Filter;
        public Func<object, object> Converter;

        public HashSet<object> visibleItems = new HashSet<object>();

        private FrameworkElement _oldSelected;
        public FrameworkElement SelectedItem
        {
            get => _oldSelected;
            set
            {
                if(_oldSelected is SelectableItem item)
                    item.Selected = false;

                _oldSelected = value;
                ((SelectableItem)_oldSelected).Selected = true;
            }
        }
        public int SelectedIndex
        {
            get
            {
                for(int i = 0; i < itemsBed.Children.Count; i++)
                {
                    if (itemsBed.Children[i] == SelectedItem)
                        return i;
                }
                return -1;
            }
        }

        public FilterableList()
        {
            Filter = DefaultFilter;
            PropertyChanged += FilterableList_PropertyChanged;
            DataContext = this;

            InitializeComponent();
        }

        public bool DefaultFilter(object obj)
        {
            if(obj is ICreatableNode node)
            {
                return node.ClassNameStatic.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
            }
            return obj.ToString().ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
        }

        private void FilterableList_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(ItemsSource)))
            {
                if(_oldItems != null)
                {
                    foreach(var item in _oldItems)
                    {
                        OnItemsChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }

                foreach(var item in ItemsSource)
                {
                    OnItemsChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

                }
                _oldItems = ItemsSource;
                UpdateFilter();
            }
        }

        protected void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            UIElement element = ToUIElement(e.NewItems[i]);
                            itemsBed.Children.Add(element);
                            visibleItems.Add(e.NewItems[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldItems.Count > 1)
                            throw new System.Exception("more than 1 item changed");
                        itemsBed.Children.RemoveAt(e.OldStartingIndex);
                        visibleItems.Remove(e.OldItems[0]);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (e.NewItems.Count > 1)
                            throw new System.Exception("more than 1 item changed");
                        visibleItems.Remove(e.OldItems[0]);
                        visibleItems.Add(e.NewItems[0]);
                        itemsBed.Children[e.NewStartingIndex] = ToUIElement(e.NewItems[0]);

                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    {
                        itemsBed.Children.Clear();
                        visibleItems.Clear();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        UIElement tmp = itemsBed.Children[e.OldStartingIndex];
                        itemsBed.Children[e.OldStartingIndex] = itemsBed.Children[e.NewStartingIndex];
                        itemsBed.Children[e.NewStartingIndex] = tmp;
                        //no need to move in visible items, it's a hashset
                    }
                    break;
            }
        }

        private UIElement ToUIElement(object item)
        {
            SelectableItem selectable = new SelectableItem(this, item);
            if (Converter != null)
                item = Converter(item);
            if (item is UIElement uielement)
            {
                selectable.InsideContent = selectable;
            }
            else
            {
                selectable.InsideContent = new TextBlock() { Text = item.ToString() };
            }
            return selectable;
        }

        public void UpdateFilter()
        {
            if (Filter == null)
                return;
            foreach (object dataItem in ItemsSource)
            {
                bool shouldBeVisible = Filter(dataItem);
                if (shouldBeVisible && !visibleItems.Contains(dataItem))
                {
                    visibleItems.Add(dataItem);
                    itemsBed.Children.Add(ToUIElement(dataItem));
                }
                if(!shouldBeVisible && visibleItems.Contains(dataItem))
                {
                    visibleItems.Remove(dataItem);
                    RemoveFromLayout(dataItem);
                }
            }

            if (SelectedIndex == -1 && itemsBed.Children.Count != 0)
                SelectedItem = (FrameworkElement)itemsBed.Children[0];
        }

        void RemoveFromLayout(object dataItem)
        {
            for (int i = 0; i < itemsBed.Children.Count; i++)
            {
                SelectableItem item = (SelectableItem)itemsBed.Children[i];
                if(item.Data == dataItem)
                {
                    itemsBed.Children.RemoveAt(i);
                    i--;
                }
            }
        }

        private void StackPanel_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int selectedIndex = SelectedIndex;

            switch (e.Key)
            {
                case Key.Up:
                    {
                        if (selectedIndex > 0)
                            SelectedItem = (FrameworkElement)itemsBed.Children[selectedIndex - 1];
                        e.Handled = true;
                    }
                    break;
                case Key.Down:
                    {
                        if (selectedIndex < itemsBed.Children.Count - 1)
                            SelectedItem = (FrameworkElement)itemsBed.Children[selectedIndex + 1];
                        e.Handled = true;
                    }
                    break;
                case Key.Return:
                    {
                        ValidateSelected();
                    }
                    break;
            }
            SelectedItem?.BringIntoView();
        }

        public void ValidateSelected()
        {
            SelectableItem item = (SelectableItem)SelectedItem;
            if(visibleItems.Contains(item.Data))
                Validated?.Invoke(item.Data);
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilter();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(itemsBed.Children.Count != 0)
                SelectedItem = (FrameworkElement)itemsBed.Children[0];
            Keyboard.Focus(searchBox);
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Cancel?.Invoke();
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                Cancel?.Invoke();
            }
        }
    }
}
