using Motio.NodeCommon.Utils;
using Motio.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Motio.UI.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        MainControlViewModel mainViewModel;

        public MainControl()
        {
            InitializeComponent();
        }

        private void MainControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(mainViewModel == null)
            {
                mainViewModel = (MainControlViewModel)DataContext;
                mainViewModel.CanvasOverEverything = canvasOverEverything;
            }
        }

        private void Switch_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            keyClipTab.SelectedIndex = keyClipTab.SelectedIndex == 1 ? 0 : 1;
        }

        private void Switch_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
