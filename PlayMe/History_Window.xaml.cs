using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace PlayMe
{
    public partial class History_Window : Window
    {
        private List<string> listeningHistory;

        public History_Window(List<string> history)
        {
            InitializeComponent();
            listeningHistory = history;

            foreach (var item in listeningHistory)
            {
                ListBoxItem listBoxItem = new ListBoxItem();
                listBoxItem.Content = item;
                History_List_Box.Items.Add(listBoxItem);
            }

            History_List_Box.MouseDoubleClick += History_List_Box_MouseDoubleClick;
        }

        private void History_List_Box_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (History_List_Box.SelectedItem != null)
            {
                string selectedTrack = ((ListBoxItem)History_List_Box.SelectedItem).Content.ToString();
                ((MainWindow)Application.Current.MainWindow).PlaySelectedTrack(selectedTrack);
                DialogResult = true;
            }
        }
    }
}
