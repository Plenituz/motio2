using Microsoft.Win32;
using Motio.Configuration;
using Motio.Graphics;
using Motio.NodeCommon.ObjectStoringImpl;
using Motio.Selecting;
using Motio.UI.Utils;
using Motio.UI.Utils.Export;
using Motio.UI.ViewModels;
using Motio.UI.Views;
using Motio.UI.Views.Dialogs;
using Motio.Undoing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Motio2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainControlViewModel mainViewModel;
        SelectionSquareHandler selectionSquareHandler;

        public MainWindow(MainControlViewModel mainViewModel)
        {
            DataContext = mainViewModel;
#if !DEBUG
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
            InitializeComponent();
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            
            UnhandleExceptionDialog exceptionDialog = new UnhandleExceptionDialog(e.Exception, this);
            try
            {
                //just for conviginence
                exceptionDialog.Owner = this;
            }
            catch (Exception) { }

            if (Configs.GetValue<bool>(Configs.DebugMode))
            {
                exceptionDialog.Show();

                if (e.Exception.Data[typeof(Microsoft.Scripting.Interpreter.InterpretedFrameInfo)]
                is IReadOnlyList<Microsoft.Scripting.Interpreter.InterpretedFrameInfo> trace)
                {
                    foreach (var frameInfo in trace)
                    {
                        if (frameInfo.DebugInfo != null)
                        {
                            FileEditingDialog fileEdit = new FileEditingDialog(null);
                            fileEdit.FilePath = frameInfo.DebugInfo.FileName;
                            fileEdit.Show();
                            fileEdit.SelectLine(frameInfo.DebugInfo.StartLine, frameInfo.DebugInfo.Index);
                        }
                    }
                }

                if (e.Exception.Data[typeof(Microsoft.Scripting.Runtime.DynamicStackFrame)]
                        is IReadOnlyList<Microsoft.Scripting.Runtime.DynamicStackFrame> frames)
                {
                    // do stuff with frames
                }
            }
            else
            {
                exceptionDialog.ShowDialog();
                Application.Current.Shutdown();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandleExceptionDialog exceptionDialog = new UnhandleExceptionDialog((Exception)e.ExceptionObject, this);
            try
            {
                //just for conviginence
                exceptionDialog.Owner = this;
            }
            catch (Exception) { }
            exceptionDialog.ShowDialog();
            Application.Current.Shutdown();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && Keyboard.FocusedElement == this) 
            {
                mainViewModel.RenderView.ExecutePlayPause();
            }
            else if (e.Key == Key.Tab)
            {
                mainViewModel.PropertyPanel.AddToTargetedNode();
            }
            else if (e.Key == Key.Delete)
            {
                bool hitSmtg = false;
                VisualTreeHelper.HitTest(this, null,
                    (o) =>
                    {
                        if(o is ISelectionClearer clearer)
                        {
                            hitSmtg = true;
                            DeleteSelectedGroups(clearer.HyperGroupsToClear);
                            return HitTestResultBehavior.Stop;
                        }
                        return HitTestResultBehavior.Continue;
                    }, 
                    new PointHitTestParameters(Mouse.GetPosition(this)));
                if (!hitSmtg)
                {
                    DeleteAllSelection();
                }
            }
            else
            {
                foreach(var pair in Selection.All())
                {
                    pair.Value.KeyPressed(e);
                    if (e.Handled)
                        break;
                }
            }
        }

        private void DeleteAllSelection()
        {
            foreach(var pair in Selection.All())
            {
                if (pair.Value.Delete())
                    Selection.Remove(pair.Key, pair.Value);
            }
        }

        private void DeleteSelectedGroups(string[] groupNames)
        {
            foreach(string group in groupNames)
            {
                foreach(var pair in Selection.ListHyperGroup(group))
                {
                    if (pair.Value.Delete())
                        Selection.Remove(pair.Key, pair.Value);
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if(mainViewModel == null)
            {
                mainViewModel = (MainControlViewModel)DataContext;
                mainViewModel.MenuQuiDecaleTout = menuQuiDecaleTout;
                //selection square has too be on the window itself, otherwise the focus 
                //gets fucked and the space bar doesn't work
                selectionSquareHandler = new SelectionSquareHandler(this);
                selectionSquareHandler.Hook(this);
                if (Configs.GetValue<bool>(Configs.ConsoleOnStart))
                {
                    ConsoleDialog console = new ConsoleDialog
                    {
                        Owner = this,
                    };
                    console.Show();
                }
                if(Configs.GetValue<bool>(Configs.FullScreenOnStart))
                {
                    WindowState = WindowState.Maximized;
                }

                foreach (ICreatableNode creatable in TimelineExporter.exporters)
                {
                    //<MenuItem Header="PNG" Click="Export_Click" Foreground="Black"/>

                    MenuItem item = new MenuItem()
                    {
                        Header = creatable.ClassNameStatic,
                        Foreground = Brushes.Black
                    };
                    item.Click += Export_Click;
                    ExportList.Items.Add(item);
                }
            }
        }

        public void ReloadWith(AnimationTimelineViewModel timeline)
        {
            theRootOfAllEvil.Children.RemoveAt(0);
            mainViewModel = new MainControlViewModel(timeline);
            DataContext = mainViewModel;
            selectionSquareHandler.UnHook(this);

            theRootOfAllEvil.Children.Add(new MainControl());
            mainViewModel.MenuQuiDecaleTout = menuQuiDecaleTout;

            selectionSquareHandler = new SelectionSquareHandler(this);
            selectionSquareHandler.Hook(this);
        }
        
        private void Configs_Click(object sender, RoutedEventArgs e)
        {
            ConfigEditDialog2 dialog = new ConfigEditDialog2
            {
                Owner = this
            };
            dialog.Show();
        }

        private void PythonFile_Click(object sender, RoutedEventArgs e)
        {
            FileEditingDialog fileEdit = new FileEditingDialog(mainViewModel.AnimationTimeline);
            fileEdit.Owner = this;
            fileEdit.Show();
            string defaultFile = Configs.GetValue<string>(Configs.DefaultEditorFile);
            if (!string.IsNullOrWhiteSpace(defaultFile))
                fileEdit.FilePath = defaultFile;
        }

        private void NewGraphicsNode_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void NewGraphicsNode_Executed(object sender, ExecutedRoutedEventArgs e) => mainViewModel.NewNode();
        private void NextFrame_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void NextFrame_Executed(object sender, ExecutedRoutedEventArgs e) => mainViewModel.AnimationTimeline.CurrentFrame++;
        private void PrevFrame_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void PrevFrame_Executed(object sender, ExecutedRoutedEventArgs e) => mainViewModel.AnimationTimeline.CurrentFrame--;
        private void OpenConsole_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void OpenConsole_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ConsoleDialog console = new ConsoleDialog
            {
                Owner = this
            };
            console.Show();
        }
        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e) => ExecuteSave(false);
        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) => ExecuteLoad();
        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = UndoStack.CanUndo();
        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = UndoStack.CanRedo();

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ProxyStatic.original2proxy.Clear();
            var t = AnimationTimelineViewModel.Create();
            ReloadWith(t);
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e) => UndoStack.Undo();
        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e) => UndoStack.Redo();

        public void ExecuteSave(bool forceDialog)
        {
            if (string.IsNullOrEmpty(mainViewModel.CurrentFile) || forceDialog)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog()
                {
                    CheckPathExists = true,
                    Filter = "Water Motion File (*.wmot)|*.wmot",
                    AddExtension = true,
                    OverwritePrompt = true
                };
                if (saveFileDialog.ShowDialog() == true && SafeSaveTimeline(saveFileDialog.FileName))
                {
                    mainViewModel.CurrentFile = saveFileDialog.FileName;
                }
            }
            else
            {
                SafeSaveTimeline(mainViewModel.CurrentFile);
            }
        }

        /// <summary>
        /// returns weither or not the save was successful
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        bool SafeSaveTimeline(string path)
        {
#if !DEBUG
            try
            {
#endif
            mainViewModel.SaveToFile(path);
            return true;
#if !DEBUG
        }
            catch (Exception e)
            {
                MessageBox.Show("Error saving file :\n" + e.Message,
                    "Error saving file", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
#endif
        }

        public void ExecuteLoad()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Water Motion File (*.wmot)|*.wmot|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ProxyStatic.original2proxy.Clear();
                var t = AnimationTimelineViewModel.CreateFromFile(openFileDialog.FileName);
                if (t == null)
                    return;
                ReloadWith(t);
                mainViewModel.CurrentFile = openFileDialog.FileName;
            }
        }

        private void ClearImgs_Click(object sender, RoutedEventArgs e)
        {
            ImageStorage.Clear();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            string name = (string)item.Header;

            ExportSequenceDialog exportDialog = new ExportSequenceDialog(mainViewModel, name);
            exportDialog.ShowDialog();
        }
    }
}
