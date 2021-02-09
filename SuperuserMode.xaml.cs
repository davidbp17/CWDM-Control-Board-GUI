using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CWDM_Control_Board_GUI
{
	/// <summary>
	/// Interaction logic for SuperuserMode.xaml
	/// </summary>
	public partial class SuperuserMode : Window
	{
		CWDM_Control_Board_GUI.MainWindow mainWindowRefrence;
		public SuperuserMode(CWDM_Control_Board_GUI.MainWindow mainWindow)
		{
			InitializeComponent();
			mainWindowRefrence = mainWindow;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			string typedPassword = SuperuserPasswordBox.Password;
			mainWindowRefrence.EnteredPassword(typedPassword);
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
