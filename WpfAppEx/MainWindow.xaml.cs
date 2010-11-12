using System.Windows;

namespace WpfAppEx
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			org.pescuma.wpfex.Grid.SetRows(myGrid, "*,Auto...,80");
		}
	}
}
