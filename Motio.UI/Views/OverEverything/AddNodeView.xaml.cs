using Motio.NodeCommon.ObjectStoringImpl;
using Motio.NodeImpl;
using Motio.UI.Utils;
using System;
using System.Windows;

namespace Motio.UI.Views.OverEverything
{
    /// <summary>
    /// TODO search bar
    /// </summary>
    public partial class AddNodeView : BaseOverEverything
    {
        public Action<ICreatableNode> onSelectedType;
        public bool autoClose = true;
        public NodeType Parameter { get; set; }

        public AddNodeView()
        {
            InitializeComponent();
            DataContext = this;//this has to be after InitializeComponent() 
            //to give time to Parameter to be set
        }

        private void AddNodeView_Loaded(object sender, RoutedEventArgs e)
        {
            filteredList.Converter = GetClassNameStatic;
            filteredList.Validated += FilteredList_Validated;
            filteredList.Cancel += FilteredList_Cancel;
            switch (Parameter)
            {
                case NodeType.Property:
                    filteredList.ItemsSource = NodeScanner.UserAccesiblePropertyNodes;
                    break;
                case NodeType.Graphics:
                    filteredList.ItemsSource = NodeScanner.UserAccesibleGraphicsNodes;
                    break;
                default:
                    throw new Exception("wrong parameter");
            }
        }

        private void FilteredList_Cancel()
        {
            Close();
        }

        private void FilteredList_Validated(object obj)
        {
            onSelectedType?.Invoke((ICreatableNode)obj);
            //IInputElement i = FocusManager.GetFocusedElement(FocusManager.GetFocusScope(this));
            Close();
        }

        private object GetClassNameStatic(object obj)
        {
            return ((ICreatableNode)obj).ClassNameStatic;
        }

        //private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    //TODO I can't believe this is in the codebase
        //    new Thread(new ThreadStart(() =>
        //    {
        //        //we have to wait for the trigger that sets the item to focus to activate 
        //        //before we can check what is actually focused
        //        Thread.Sleep(50);
        //        try
        //        {
        //            Application.Current.Dispatcher.Invoke(CheckFocus);
        //        }
        //        catch (Exception) { }

        //    }))
        //    { IsBackground = true }.Start();
        //}

        //private void CheckFocus()
        //{
        //    if (searchBar.IsFocused)
        //        return;
        //    CreatableNode item = (CreatableNode)listBox.SelectedItem;
        //    ListBoxItem lbi = (ListBoxItem)listBox
        //        .ItemContainerGenerator.ContainerFromItem(item);
        //    if (lbi == null || !lbi.IsFocused)
        //    {
        //        Close();
        //    }
        //}

        //private void ListViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    ListBox listbox = sender as ListBox;
        //    CreatableNode item = listbox.SelectedItem as CreatableNode;
        //    if (item != null)
        //    {
        //        onSelectedType?.Invoke(item);
        //        if (autoClose)
        //        {
        //            Close();
        //        }
        //    }
        //    else
        //    {
        //        //no choice selected when enter was pushed 
        //    }
        //}

        //private void EnterPressed_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}

        //private void EnterPressed_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    ListViewItem_PreviewMouseLeftButtonUp(listBox, null);
        //}

        //private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    listBox.Items.Filter = listBox.Items.Filter;
        //}
    }
}
