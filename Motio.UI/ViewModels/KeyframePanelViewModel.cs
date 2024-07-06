using Motio.Configuration;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using WPFCustomMessageBox;

namespace Motio.UI.ViewModels
{
    public class KeyframePanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public MainControlViewModel mainViewModel;

        /// <summary>
        /// these values are in frame
        /// determines the bounds of what is displayed in the keyframe view 
        /// </summary>
        public double Left { get; set; } = 0;
        public double Right { get; set; } = 100;

        public ObservableCollection<NodeViewModel> NodeInTimeline { get; set; } = new ObservableCollection<NodeViewModel>();

        public KeyframePanelViewModel(MainControlViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        public void AddToTimeline(NodeViewModel node)
        {
            if (node == null)
            {
                MessageBox.Show("tried to bring a null node to the timeline");
                return;
            }

            int index = NodeInTimeline.IndexOf(node);
            if (index != -1)
            {
                NodeInTimeline.Move(index, 0);
            }
            else
            {
                if (node.PropertiesInTimeline.Count != 0)
                {
                    NodeInTimeline.Insert(0, node);
                }
                else if ((bool)Preferences.GetValue(Preferences.ShowWarning_NoPropertyForTL).value)
                {
                    MessageBoxResult result = CustomMessageBox.ShowOKCancel(
                        "This node doesn't have any property to display in the Timeline selected!",
                        "No property selected", "OK", "Stop telling me", MessageBoxImage.Exclamation);
                    if (result == MessageBoxResult.Cancel)
                    {
                        Preferences.SetValue(Preferences.ShowWarning_NoPropertyForTL, false);
                        Preferences.Save();
                    }

                }
            }
        }

        public void RemoveFromTimeline(NodeViewModel node)
        {
            if (node == null)
            {
                MessageBox.Show("tried to remove null node from the timeline");
                return;
            }

            NodeInTimeline.Remove(node);
        }
    }
}
