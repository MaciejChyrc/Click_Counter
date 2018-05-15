using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Click_counter
{
    static class DataTransfer
    {
		public static void CopyDictionaryKeys (Dictionary<string, int> from, Dictionary<string, int> to)
		{
			foreach (KeyValuePair<string, int> pair in from)
			{
				if (!to.ContainsKey (pair.Key))
					to.Add (pair.Key, 0);
			}
		}

		public static void ReadFileToDictionaries (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

			if (File.Exists (dir + filename))
			{
				StreamReader file = new StreamReader (dir + filename);
				string lineToSplit;
				while (!String.IsNullOrEmpty (lineToSplit = file.ReadLine ()))
				{
					string[] keyVal = lineToSplit.Split (':');
					keyVal[0] = keyVal[0].TrimEnd ();
					keyVal[1] = keyVal[1].TrimStart ();

					if (!mouseDict.ContainsKey (keyVal[0]))
						mouseDict.Add (keyVal[0], int.Parse (keyVal[1]));
				}

				while (!file.EndOfStream)
				{
					lineToSplit = file.ReadLine ();

					if (lineToSplit != null)
					{
						string[] keyVal = lineToSplit.Split (':');
						keyVal[0] = keyVal[0].TrimEnd ();
						keyVal[1] = keyVal[1].TrimStart ();

						if (!keysDict.ContainsKey (keyVal[0]))
							keysDict.Add (keyVal[0], int.Parse (keyVal[1]));
					}
				}
			}
		}

		public static void SaveLogs (string filename, Dictionary<string, int> keysDict, Dictionary<string, int> mouseDict)
		{
			string dir = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";

			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			StreamWriter file = new StreamWriter (dir + filename);

			foreach (KeyValuePair<string, int> pair in mouseDict.OrderByDescending (x => x.Value))
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
	}
}