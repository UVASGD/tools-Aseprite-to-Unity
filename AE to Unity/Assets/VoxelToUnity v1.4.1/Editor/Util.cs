namespace Voxel2Unity {

	using UnityEngine;
	using UnityEditor;
	using System.Collections;
	using System.IO;


	public struct Util {



		#region --- File ---



		public static string Load (string _path) {
			try {
				StreamReader _sr = File.OpenText(_path);
				string _data = _sr.ReadToEnd();
				_sr.Close();
				return _data;
			} catch (System.Exception) {
				return "";
			}
		}



		public static void Save (string _data, string _path) {
			try {
				FileStream fs = new FileStream(_path, FileMode.Create);
				StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
				sw.Write(_data);
				sw.Close();
				fs.Close();
			} catch (System.Exception) {
				return;
			}
		}



		public static byte[] FileToByte (string path) {
			if (File.Exists(path)) {
				byte[] bytes = null;
				try {
					bytes = File.ReadAllBytes(path);
				} catch {
					return null;
				}
				return bytes;
			} else {
				return null;
			}
		}



		public static bool ByteToFile (byte[] bytes, string path) {
			try {
				string parentPath = new FileInfo(path).Directory.FullName;
				CreateFolder(parentPath);
				FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
				fs.Write(bytes, 0, bytes.Length);
				fs.Close();
				fs.Dispose();
				return true;
			} catch {
				return false;
			}
		}



		public static void CreateFolder (string _path) {
			_path = GetFullPath(_path);
			if (Directory.Exists(_path))
				return;
			string _parentPath = new FileInfo(_path).Directory.FullName;
			if (Directory.Exists(_parentPath)) {
				Directory.CreateDirectory(_path);
			} else {
				CreateFolder(_parentPath);
				Directory.CreateDirectory(_path);
			}
		}



		#endregion



		#region --- Path ---



		public static string FixPath (string _path) {
			_path = _path.Replace('\\', '/');
			_path = _path.Replace("//", "/");
			while (_path.Length > 0 && _path[0] == '/') {
				_path = _path.Remove(0, 1);
			}
			return _path;
		}



		public static string GetFullPath (string path) {
			return new FileInfo(path).FullName;
		}



		public static string RelativePath (string path) {
			path = FixPath(path);
			if (path.StartsWith("Assets")) {
				return path;
			}
			if (path.StartsWith(FixPath(Application.dataPath))) {
				return "Assets" + path.Substring(FixPath(Application.dataPath).Length);
			} else {
				return "";
			}
		}



		public static string CombinePaths (params string[] paths) {
			string path = "";
			for (int i = 0; i < paths.Length; i++) {
				path = Path.Combine(path, FixPath(paths[i]));
			}
			return FixPath(path);
		}



		public static string GetExtension (string path) {
			return Path.GetExtension(path);
		}



		public static string GetName (string path) {
			return Path.GetFileNameWithoutExtension(path);
		}



		public static string ChangeExtension (string path, string newEx) {
			return Path.ChangeExtension(path, newEx);
		}



		public static bool PathIsDirectory (string path) {
			FileAttributes attr = File.GetAttributes(path);
			if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
				return true;
			else
				return false;
		}



		#endregion



		#region --- MSG ---


		public static bool Dialog (string title, string msg, string ok, string cancel = "") {
			EditorApplication.Beep();
			PauseWatch();
			if (string.IsNullOrEmpty(cancel)) {
				bool sure = EditorUtility.DisplayDialog(title, msg, ok);
				RestartWatch();
				return sure;
			} else {
				bool sure = EditorUtility.DisplayDialog(title, msg, ok, cancel);
				RestartWatch();
				return sure;
			}
		}


		public static void ProgressBar (string title, string msg, float value) {
			value = Mathf.Clamp01(value);
			EditorUtility.DisplayProgressBar(title, msg, value);
		}


		public static void ClearProgressBar () {
			EditorUtility.ClearProgressBar();
		}


		#endregion



		#region --- Watch ---


		private static System.Diagnostics.Stopwatch TheWatch;


		public static void StartWatch () {
			TheWatch = new System.Diagnostics.Stopwatch();
			TheWatch.Start();
		}


		public static void PauseWatch () {
			if (TheWatch != null) {
				TheWatch.Stop();
			}
		}


		public static void RestartWatch () {
			if (TheWatch != null) {
				TheWatch.Start();
			}
		}


		public static double StopWatchAndGetTime () {
			if (TheWatch != null) {
				TheWatch.Stop();
				return TheWatch.Elapsed.TotalSeconds;
			}
			return 0f;
		}


		#endregion



	}

}
