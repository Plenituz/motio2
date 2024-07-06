using Motio.Configuration;
using Motio.UI.Views.ConfigViews;
using Motio.UICommon;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Motio.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ConfigEditDialog2.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ConfigEditDialog2 : Window
    {
        public IEnumerable<string> CategoryList
        {
            get
            {
                HashSet<string> categories = new HashSet<string>();

                foreach(ConfigEntry entry in Configs.Instance.Entries)
                {
                    if (!categories.Contains(entry.Category))
                    {
                        categories.Add(entry.Category);
                    }
                }
                var list = categories.ToList();
                list.Sort();
                return list;
            }
        }

        public IEnumerable<ConfigEntry> AllEntries => Configs.Instance.Entries;
        public string SelectedCategory { get; set; }


        public ConfigEditDialog2()
        {
            SelectedCategory = CategoryList.First();
            DataContext = this;
            InitializeComponent();
        }

        private void SaveAll()
        {
            bool close = true;
            for (int i = 0; i < EntriesList.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)EntriesList.ItemContainerGenerator.ContainerFromIndex(i);
                ConfigViewBase entryUi = ToolBox.FindVisualChild<ConfigViewBase>(presenter);
                ConfigEntry entry = (ConfigEntry)EntriesList.Items[i];

                bool passed;
                string error;
                try
                {
                    (passed, error) = entry.SetValue(entryUi.GetUserInputedValue());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error converting value for " + entry.ShortName + " (in category " + entry.Category + "):" +
                                    "\n" + ex.Message);
                    close = false;
                    continue;
                }

                if (!passed)
                {
                    MessageBox.Show("Error setting config value for " + entry.ShortName + " (in category " + entry.Category + "):" +
                                    "\n" + error);
                    close = false;
                }
            }
            Configs.SaveSafe();
            if(close)
                Close();
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SelectedCategory = (string)e.AddedItems[0];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveAll();
        }
    }
}
