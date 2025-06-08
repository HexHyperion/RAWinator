using System.Windows;

namespace rawinator
{
    public partial class InputDialog : Window
    {
        public string ResponseText => InputBox.Text;

        public InputDialog(string prompt)
        {
            InitializeComponent();
            PromptText.Text = prompt;
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}