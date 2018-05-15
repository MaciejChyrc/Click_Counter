using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Gma.System.MouseKeyHook;

using WpfLabel = System.Windows.Controls.Label;

namespace Click_counter
{
	/// <summary>
	/// Logika interakcji dla klasy MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IKeyboardMouseEvents globalHook;
		private Dictionary<string, int> keysClickCount;
		private Dictionary<string, int> mouseClickCount;
		private Dictionary<string, int> todayKeysClickCount;
		private Dictionary<string, int> todayMouseClickCount;
		private Dictionary<string, bool> keyHoldLock;
		private List<WpfLabel> listOfLabels;
		private bool showTodayClicks = false;
		private string appLaunchDate;
		private string appLaunchTime;

		public MainWindow ()
		{
			globalHook = Hook.GlobalEvents ();
			InitializeComponent ();
			listOfLabels = GetLabelList();
			keysClickCount = new Dictionary<string, int>();
			mouseClickCount = new Dictionary<string, int>();
			todayKeysClickCount = new Dictionary<string, int> ();
			todayMouseClickCount = new Dictionary<string, int> ();
			keyHoldLock = new Dictionary<string, bool>();
			FillKeyHoldLockDict();
			appLaunchDate = DateTime.Now.ToShortDateString();
			appLaunchTime = DateTime.Now.Hour + "-" + DateTime.Now.Minute;
			DataTransfer.ReadFileToDictionaries (appLaunchDate + ".txt", todayKeysClickCount, todayMouseClickCount);
			DataTransfer.CopyDictionaryKeys (todayMouseClickCount, mouseClickCount);
			DataTransfer.CopyDictionaryKeys (todayKeysClickCount, keysClickCount);
			//Mouse position tracking is always on
			globalHook.MouseMoveExt += (sender, e) =>
			{
				MousePos.Content = $"{e.X}, {e.Y}";
			};
			//globalHook = Hook.GlobalEvents();
			//SubscribeKeyboardMouseEvents();
		}

		private void SubscribeKeyboardMouseEvents ()
		{
			globalHook.MouseDownExt += GlobalHookMouseDownExt;
			globalHook.KeyDown += GlobalHookKeyDown;
			globalHook.KeyUp += GlobalHookKeyUp;
		}

		private void UnsubscribeKeyboardMouseEvents ()
		{
			globalHook.MouseDownExt -= GlobalHookMouseDownExt;
			globalHook.KeyDown -= GlobalHookKeyDown;
			globalHook.KeyUp -= GlobalHookKeyUp;
		}

		private void GlobalHookMouseDownExt (object sender, MouseEventExtArgs e)
		{
			if (mouseClickCount.ContainsKey (e.Button.ToString ()))
			{
				mouseClickCount[e.Button.ToString ()]++;
				todayMouseClickCount[e.Button.ToString ()]++;
			}
			else if (!mouseClickCount.ContainsKey(e.Button.ToString()))
			{
				mouseClickCount.Add(e.Button.ToString(), 1);
				if (!todayMouseClickCount.ContainsKey (e.Button.ToString ()))
				{
					todayMouseClickCount.Add (e.Button.ToString (), 1);
				}
			}
			
			if (!showTodayClicks)
			{
				if (e.Button == MouseButtons.Left)
					LMB.Content = mouseClickCount[e.Button.ToString()];
				else if (e.Button == MouseButtons.Right)
					RMB.Content = mouseClickCount[e.Button.ToString()];
				else if (e.Button == MouseButtons.Middle)
					Middle.Content = mouseClickCount[e.Button.ToString()];
				else if (e.Button == MouseButtons.XButton1)
					XButton1.Content = mouseClickCount[e.Button.ToString()];
				else if (e.Button == MouseButtons.XButton2)
					XButton2.Content = mouseClickCount[e.Button.ToString()];
				//---
				Clicks.Content = $"Clicked button: {e.Button}, number of clicks: {mouseClickCount[e.Button.ToString ()]}";
			}
			else
			{
				if (e.Button == MouseButtons.Left)
					LMB.Content = todayMouseClickCount[e.Button.ToString ()];
				else if (e.Button == MouseButtons.Right)
					RMB.Content = todayMouseClickCount[e.Button.ToString ()];
				else if (e.Button == MouseButtons.Middle)
					Middle.Content = todayMouseClickCount[e.Button.ToString ()];
				else if (e.Button == MouseButtons.XButton1)
					XButton1.Content = todayMouseClickCount[e.Button.ToString ()];
				else if (e.Button == MouseButtons.XButton2)
					XButton2.Content = todayMouseClickCount[e.Button.ToString ()];
			}
		}

		private void GlobalHookKeyDown (object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (keysClickCount.ContainsKey(e.KeyCode.ToString()) && !keyHoldLock[e.KeyCode.ToString()])
			{
				keysClickCount[e.KeyCode.ToString()]++;
				todayKeysClickCount[e.KeyCode.ToString ()]++;
				keyHoldLock[e.KeyCode.ToString ()] = true;
			}
			else if (!keysClickCount.ContainsKey (e.KeyCode.ToString()))
			{
				keysClickCount.Add(e.KeyCode.ToString(), 1);
				keyHoldLock[e.KeyCode.ToString ()] = true;
				if (!todayKeysClickCount.ContainsKey (e.KeyCode.ToString ()))
				{
					todayKeysClickCount.Add(e.KeyCode.ToString(), 1);
				}
			}
			//---
			Clicks.Content = $"Clicked button: {e.KeyCode}, number of clicks: {keysClickCount[e.KeyCode.ToString()]}";
			WpfLabel foundLabel = (WpfLabel) FindName(e.KeyCode.ToString());

			if (!showTodayClicks && foundLabel != null)
				foundLabel.Content = keysClickCount[e.KeyCode.ToString()];
			else if (showTodayClicks && foundLabel != null)
				foundLabel.Content = todayKeysClickCount[e.KeyCode.ToString ()];

			if (e.KeyCode == Keys.Return)
				NumPadReturn.Content = Return.Content;
		}

		private void GlobalHookKeyUp (object sender, System.Windows.Forms.KeyEventArgs e)
		{
			keyHoldLock[e.KeyCode.ToString ()] = false;
		}

		private void Window_Closing (object sender, System.ComponentModel.CancelEventArgs e)
		{
			UnsubscribeKeyboardMouseEvents();
			string currentDate = DateTime.Now.ToShortDateString ();
			string currentTime = DateTime.Now.Hour + "-" + DateTime.Now.Minute;
			string currentSessionLogFilename = appLaunchDate + "_" + appLaunchTime + "_to_" + currentDate + "_" + currentTime + ".txt";
			string currentDayLogFilename = appLaunchDate + ".txt";
			DataTransfer.SaveLogs (currentSessionLogFilename, keysClickCount, mouseClickCount);
			DataTransfer.SaveLogs (currentDayLogFilename, todayKeysClickCount, todayMouseClickCount);
		}

		/*private void SaveLogs (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			StreamWriter file = new StreamWriter (dir + filename);

			foreach (KeyValuePair<string, int> pair in mouseDict.OrderByDescending(x => x.Value))
			{
				string pairFormattedString = String.Format ("{0,-20}: {1, 7}", pair.Key, pair.Value.ToString ());
				file.WriteLine (pairFormattedString);
			}

			file.WriteLine ("");

			foreach (KeyValuePair<string, int> pair in keysDict.OrderByDescending (x => x.Value))
			{
				string pairFormattedString = String.Format ("{0,-20}: {1, 7}", pair.Key, pair.Value.ToString ());
				file.WriteLine (pairFormattedString);
			}
			file.Close ();
		}*/

		/*private void ReadFileToDictionaries (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

			if (File.Exists (dir + filename))
			{
				StreamReader file = new StreamReader(dir + filename);
				string lineToSplit;
				while (!String.IsNullOrEmpty(lineToSplit = file.ReadLine ()))
				{
					string[] keyVal = lineToSplit.Split(':');
					keyVal[0] = keyVal[0].TrimEnd();
					keyVal[1] = keyVal[1].TrimStart();

					if (!mouseDict.ContainsKey (keyVal[0]))
						mouseDict.Add(keyVal[0], int.Parse(keyVal[1]));
				}

				while (!file.EndOfStream)
				{
					lineToSplit = file.ReadLine();

					if (lineToSplit != null)
					{
						string[] keyVal = lineToSplit.Split (':');
						keyVal[0] = keyVal[0].TrimEnd();
						keyVal[1] = keyVal[1].TrimStart();

						if (!keysDict.ContainsKey(keyVal[0]))
							keysDict.Add (keyVal[0], int.Parse (keyVal[1]));
					}
				}
			}
		}*/

		private void FillKeyHoldLockDict ()
		{
			foreach (var key in Enum.GetValues(typeof(Keys)))
			{
				if (!keyHoldLock.ContainsKey(key.ToString()))
					keyHoldLock.Add(key.ToString(), false);
			}
		}

		/*private void CopyDictionaryKeys (Dictionary<string, int> from, Dictionary<string, int> to)
		{
			foreach (KeyValuePair<string, int> pair in from)
			{
				if (!to.ContainsKey(pair.Key))
					to.Add(pair.Key, 0);
			}
		}*/

		private void EvtListeningSwitch_Checked (object sender, RoutedEventArgs e)
		{
			SubscribeKeyboardMouseEvents();
		}

		private void EvtListeningSwitch_Unchecked (object sender, RoutedEventArgs e)
		{
			UnsubscribeKeyboardMouseEvents();
		}

		private void dailyCount_Checked (object sender, RoutedEventArgs e)
		{
			showTodayClicks = true;
			
			SwitchUiLabels (true);			
		}

		private void dailyCount_Unchecked (object sender, RoutedEventArgs e)
		{
			showTodayClicks = false;
			
			SwitchUiLabels (false);			
		}

		private void SwitchUiLabels (bool daily)
		{
			if (!daily)
			{
				foreach (var label in listOfLabels)
				{
					if (label.Name == "LMB")
					{
						if (mouseClickCount.ContainsKey ("Left"))
							label.Content = mouseClickCount["Left"];
						RefreshLabel(label);
					}
					else if (label.Name == "RMB")
					{
						if (mouseClickCount.ContainsKey ("Right"))
							label.Content = mouseClickCount["Right"];
						RefreshLabel (label);
					}
					else if (label.Name == "Middle")
					{
						if (mouseClickCount.ContainsKey (label.Name))
							label.Content = mouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else if (label.Name == "XButton1")
					{
						if (mouseClickCount.ContainsKey (label.Name))
							label.Content = mouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else if (label.Name == "XButton2")
					{
						if (mouseClickCount.ContainsKey (label.Name))
							label.Content = mouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else
					{
						if (keysClickCount.ContainsKey (label.Name))
							label.Content = keysClickCount[label.Name];
						RefreshLabel (label);
					}
				}
			}
			else
			{
				foreach (var label in listOfLabels)
				{
					if (label.Name == "LMB")
					{
						if (todayMouseClickCount.ContainsKey("Left"))
							label.Content = todayMouseClickCount["Left"];
						RefreshLabel (label);
					}
					else if (label.Name == "RMB")
					{
						if (todayMouseClickCount.ContainsKey ("Right"))
							label.Content = todayMouseClickCount["Right"];
						RefreshLabel (label);
					}
					else if (label.Name == "Middle")
					{
						if (todayMouseClickCount.ContainsKey (label.Name))
							label.Content = todayMouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else if (label.Name == "XButton1")
					{
						if (todayMouseClickCount.ContainsKey (label.Name))
							label.Content = todayMouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else if (label.Name == "XButton2")
					{
						if (todayMouseClickCount.ContainsKey (label.Name))
							label.Content = todayMouseClickCount[label.Name];
						RefreshLabel (label);
					}
					else
					{
						if (todayKeysClickCount.ContainsKey (label.Name))
							label.Content = todayKeysClickCount[label.Name];
						RefreshLabel (label);
					}
				}
			}
		}

		private List<WpfLabel> GetLabelList ()
		{
			List<WpfLabel> temp = new List<WpfLabel>();
			WpfLabel foundLabel;

			foreach (var keyCode in Enum.GetValues(typeof(Keys)))
			{
				foundLabel = (WpfLabel) FindName(keyCode.ToString());
				if (foundLabel != null)
					temp.Add(foundLabel);
			}
			foundLabel = (WpfLabel) FindName("LMB");
			temp.Add (foundLabel);
			foundLabel = (WpfLabel) FindName ("RMB");
			temp.Add (foundLabel);
			foundLabel = (WpfLabel) FindName ("Middle");
			temp.Add (foundLabel);
			foundLabel = (WpfLabel) FindName ("XButton1");
			temp.Add (foundLabel);
			foundLabel = (WpfLabel) FindName ("XButton2");
			temp.Add (foundLabel);

			return temp;
		}

		private void RefreshLabel (UIElement label)
		{
			label.Dispatcher.Invoke(DispatcherPriority.Render, UpdateLabel);
		}

		private Action UpdateLabel = delegate { };
	}
}