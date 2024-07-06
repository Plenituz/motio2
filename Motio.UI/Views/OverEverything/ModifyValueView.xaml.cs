using System;
using System.Windows;
using System.Windows.Input;

namespace Motio.UI.Views.OverEverything
{
    /// <summary>
    /// Logique d'interaction pour ModifyValueView.xaml
    /// </summary>
    public partial class ModifyValueView : BaseOverEverything
    {
        public string DefaultValue { get; set; }
        public double DefaultWidth { get; set; }
        public Func<string, Exception> TrySetPropertyValue;
        private bool setValue = true;

        public ModifyValueView()
        {
            InitializeComponent();
            DataContext = this;
        }

        protected override void BaseOverEverything_Loaded(object sender, RoutedEventArgs e)
        {
            base.BaseOverEverything_Loaded(sender, e);
            Y -= ActualHeight;
            textBox.SelectAll();
            textBox.Focus();
        }

        private void SetValue()
        {
            if (TrySetPropertyValue != null)
            {
                Exception ex = TrySetPropertyValue(textBox.Text);
                if (ex != null)
                {
                    MessageBox.Show("Invalid value:\n" + ex.Message, "Invalid value");
                }
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                //SetValue();
                Close();
            }
            if(e.Key == Key.Escape)
            {
                setValue = false;
                Close();
            }
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if(setValue)
                SetValue(); 
            Close();
        }
    }
}
