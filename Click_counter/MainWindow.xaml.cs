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

	//USTANDARYZOWAC NAZWY PLIKOW I ICH WYKORZYSTANIE
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
			ReadFileToDictionaries(appLaunchDate + ".txt", todayKeysClickCount, todayMouseClickCount);
			CopyDictionaryKeys(todayMouseClickCount, mouseClickCount);
			CopyDictionaryKeys(todayKeysClickCount, keysClickCount);
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
			//dodac przelaczanie miedzy zwyklymi a dziennymi klikami
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


		//dodac przelaczenie miedzy zwyklymi a dziennymi klikami
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
			
			//Clicks.Content = $"Wciśnięty klawisz: {e.KeyCode}, ilość dotychczasowych kliknięć: {keysClickCount[e.KeyCode.ToString()]}";
			WpfLabel foundLabel = (WpfLabel) FindName(e.KeyCode.ToString());//

			if (!showTodayClicks)
				foundLabel.Content = keysClickCount[e.KeyCode.ToString()];
			else
				foundLabel.Content = todayKeysClickCount[e.KeyCode.ToString ()];

			if (e.KeyCode == Keys.Return)
				NumPadReturn.Content = Return.Content;
		}

		private void GlobalHookKeyUp (object sender, System.Windows.Forms.KeyEventArgs e)
		{
			keyHoldLock[e.KeyCode.ToString ()] = false;
		}

		private string GetWindowsUsername ()
		{
			return Environment.UserName;
		}

		private void Window_Closing (object sender, System.ComponentModel.CancelEventArgs e)
		{
			UnsubscribeKeyboardMouseEvents();
			string currentDate = DateTime.Now.ToShortDateString ();
			string currentTime = DateTime.Now.Hour + "-" + DateTime.Now.Minute;
			string currentSessionLogFilename = appLaunchDate + "_" + appLaunchTime + "_to_" + currentDate + "_" + currentTime + ".txt";
			string currentDayLogFilename = appLaunchDate + ".txt";
			SaveLogs (currentSessionLogFilename, keysClickCount, mouseClickCount);
			SaveLogs (currentDayLogFilename, todayKeysClickCount, todayMouseClickCount);
		}

		private void SaveLogs (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			//string currentDate = DateTime.Now.ToShortDateString ();
			//string currentTime = DateTime.Now.Hour + "-" + DateTime.Now.Minute;
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
			//string filename = appLaunchDate + "_" + appLaunchTime + "_to_" + currentDate + "_" + currentTime + ".txt";
			//string filename = currentDate + ".txt";

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
		}

		private void ReadFileToDictionaries (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			//string currentDate = DateTime.Now.ToShortDateString ();
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
			//string filename = currentDate + ".txt";

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
				/*foreach (KeyValuePair<string, int> pair in todayMouseClickCount)
				{
					Clicks.Content += pair.Key + pair.Value + "\n";
				}
				foreach (KeyValuePair<string, int> pair in todayKeysClickCount)
				{
					Clicks.Content += pair.Key + pair.Value + "\n";
				}*/
			}
		}

		private void FillKeyHoldLockDict ()
		{
			foreach (var key in Enum.GetValues(typeof(Keys)))
			{
				if (!keyHoldLock.ContainsKey(key.ToString()))
					keyHoldLock.Add(key.ToString(), false);
			}
		}

		private void CopyDictionaryKeys (Dictionary<string, int> from, Dictionary<string, int> to)
		{
			foreach (KeyValuePair<string, int> pair in from)
			{
				if (!to.ContainsKey(pair.Key))
					to.Add(pair.Key, 0);
			}
		}

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

		/*private void SwitchUiLabels (bool daily)
		{
			WpfLabel foundLabel;
			UpdateLabel update;

			if (!daily)
			{
				foreach (KeyValuePair<string, int> pair in mouseClickCount)
				{
					if (pair.Key == "Left")
					{
						foundLabel = (WpfLabel) FindName ("LMB");
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
					else if (pair.Key == "Right")
					{
						foundLabel = (WpfLabel) FindName ("RMB");
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
					else
					{
						foundLabel = (WpfLabel) FindName (pair.Key);
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
				}
				foreach (KeyValuePair<string, int> pair in keysClickCount)
				{
					foundLabel = (WpfLabel) FindName (pair.Key);
					if (foundLabel != null)
					{
						//foundLabel.Content = pair.Value;
						update = () => foundLabel.Content = pair.Value;
						Action action = (() =>
						{
							foundLabel.Dispatcher.Invoke (update);
						});
						action.BeginInvoke (null, null);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<string, int> pair in todayMouseClickCount)
				{
					if (pair.Key == "Left")
					{
						foundLabel = (WpfLabel) FindName ("LMB");
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
					else if (pair.Key == "Right")
					{
						foundLabel = (WpfLabel) FindName ("RMB");
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
					else
					{
						foundLabel = (WpfLabel) FindName (pair.Key);
						if (foundLabel != null)
						{
							//foundLabel.Content = pair.Value;
							update = () => foundLabel.Content = pair.Value;
							Action action = (() =>
							{
								foundLabel.Dispatcher.Invoke (update);
							});
							action.BeginInvoke (null, null);
						}
					}
				}
				foreach (KeyValuePair<string, int> pair in todayKeysClickCount)
				{
					foundLabel = (WpfLabel) FindName (pair.Key);
					if (foundLabel != null)
					{
						//foundLabel.Content = pair.Value;
						update = () => foundLabel.Content = pair.Value;
						Action action = (() =>
						{
							foundLabel.Dispatcher.Invoke (update);
						});
						action.BeginInvoke (null, null);
						//DispatcherFrame frame = new DispatcherFrame ();
						//Dispatcher.BeginInvoke (DispatcherPriority.Background, (SendOrPostCallback) delegate
						//{
						//	foundLabel.Content = pair.Value;
						//}, frame);
						//Dispatcher.PushFrame (frame);
					}
				}
			}
		}*/

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

		private Action UpdateLabel = delegate {  };

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

		/*public static T FindChild<T> (DependencyObject parent, string childName) where T : DependencyObject
		{
			// Confirm parent and childName are valid. 
			if (parent == null)
				return null;

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount (parent);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild (parent, i);
				// If the child is not of the request child type child
				T childType = child as T;
				if (childType == null)
				{
					// recursively drill down the tree
					foundChild = FindChild<T> (child, childName);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null)
						break;
				}
				else if (!string.IsNullOrEmpty (childName))
				{
					var frameworkElement = child as FrameworkElement;
					// If the child's name is set for search
					if (frameworkElement != null && frameworkElement.Name == childName)
					{
						// if the child's name is of the request name
						foundChild = (T) child;
						break;
					}
				}
				else
				{
					// child element found.
					foundChild = (T) child;
					break;
				}
			}

			return foundChild;
		}*/
	}
}