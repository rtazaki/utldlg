using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace utldlg
{
	/// <summary>
	/// Description of Log.
	/// </summary>
	public class Log
	{
		private readonly string logdir = AppDomain.CurrentDomain.BaseDirectory + "Log";
		private readonly string logfile;
		public Log(string filename)
		{
			var today = DateTime.Now.ToString("yyyyMMdd");
			logfile = logdir + "\\" + filename + "_" + today + ".log";
		}
		//タイムスタンプを押して、追記書き込みする
		public void AppendAllText(string msg)
		{
			if (!Directory.Exists(logdir)) {
				Directory.CreateDirectory(logdir);
			}
			File.AppendAllText(logfile, DateTime.Now.ToString("G") + "\t" + msg + "\r\n");
		}
		//テキストをそのまま書き込む
		public void WriteAllLines(IEnumerable<string> msg)
		{
			if (!Directory.Exists(logdir)) {
				Directory.CreateDirectory(logdir);
			}
			File.WriteAllLines(logfile, msg);
			if (!msg.Any()) {
				File.Delete(logfile);
			}
		}
	}
}
