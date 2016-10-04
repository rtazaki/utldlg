using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

//ファイル選択ダイアログ風フォルダ選択ダイアログ
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

using System.IO;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace utldlg
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
		}
		
		private int retry;
		private int Retry {
			get { return retry; }
			set {
				if (value < 3) {
					retry = value;
				} else {
					tracelog.AppendAllText(filelist[i] + ": retried 3 times. skip.");
					retry = 0;
					i++;
				}
			}
		}
		private bool suspend;
		private int i;
		private List<string> filelist = new List<string>();
		private List<string> comp = new List<string>();
		
		//日付を跨いだときに別ファイルになってもいいなら、都度作成すればよい。
		private Log tracelog = new Log("TraceLog");
		private Log notcomp = new Log("NotComp");
		
		private CancellationTokenSource tokenSource;
		//再帰関数
		private async void RecursiveFunction()
		{
			var timer = new DispatcherTimer(DispatcherPriority.Normal);
			timer.Interval = new TimeSpan(0, 0, 5);
			timer.Tick += new EventHandler(dispatcherTimer_Tick);
			
			menufile.Visibility = Visibility.Collapsed;
			menuoption.Visibility = Visibility.Hidden;
			
			while (i < filelist.Count()) {
				label.Content = i + " / " + filelist.Count();
				pbar.Value = i;
				notcomp.WriteAllLines(filelist.Except(comp));
				//label更新してから、suspend処理に入ったほうが、下で楽できる。
				if (suspend) {
					break;
				}
				timer.Start();
				tokenSource = new CancellationTokenSource();
				try {
					tracelog.AppendAllText(filelist[i] + " begin");
					await Task.Run(() => {
						HeavyTestClass.Instance.TestCase1(filelist[i], tokenSource);
					}, tokenSource.Token);
					comp.Add(filelist[i]);
					tracelog.AppendAllText(filelist[i] + " finish");
					Retry = 0;
					i++;
				} catch (InvalidOperationException e) {
					tracelog.AppendAllText("couldn't retry case. skip." + "\r\n" + e.ToString());
					Retry = 0;
					i++;
				} catch (Exception e) {
					tracelog.AppendAllText("try retry case." + "\r\n" + e.ToString());
					Retry++;
				}
				timer.Stop();
			}
			//while後
			notcomp.WriteAllLines(filelist.Except(comp));
			if (i == filelist.Count()) {
				label.Content = "All Finished.";
				pbar.Value = i;
				menufile.Visibility = Visibility.Visible;
			} else if (suspend) {
				label.Content += " [Suspend]";
				pbar.Value = i;
				menuoption.Visibility = Visibility.Visible;
			}
		}
		
		private void dispatcherTimer_Tick(object sender, EventArgs e)
		{
			(sender as DispatcherTimer).Stop();
			tokenSource.Cancel();
			tracelog.AppendAllText(filelist[i] + ": timeout. retry.");
		}
		
		private SearchOption so;
		//サブフォルダを含む,含まない
		private string[] ignore;
		//除外する文字列
		/// <summary>
		/// ディレクトリ検索
		/// </summary>
		/// <returns></returns>
		private string getDirectoryPath(string title)
		{
			var dlg = new CommonOpenFileDialog();
			dlg.Title = title;
			
			var radio = new CommonFileDialogRadioButtonList();
			radio.Items.Add(new CommonFileDialogRadioButtonListItem("AllDirectories"));
			radio.Items.Add(new CommonFileDialogRadioButtonListItem("TopDirectoryOnly"));
			radio.SelectedIndex = 0;
			
			var txt = new CommonFileDialogTextBox();
			txt.Text = "BAK;__;コピー";
			
			dlg.Controls.Add(radio);
			dlg.Controls.Add(new CommonFileDialogLabel("Ignore Keywords"));
			dlg.Controls.Add(txt);
			dlg.IsFolderPicker = true;
			// 読み取り専用フォルダ,コントロールパネルは開かない
			dlg.EnsureReadOnly = false;
			dlg.AllowNonFileSystemItems = false;
			if (dlg.ShowDialog() == CommonFileDialogResult.Ok) {
				if (radio.SelectedIndex == 0) {
					so = SearchOption.AllDirectories;
				} else if (radio.SelectedIndex == 1) {
					so = SearchOption.TopDirectoryOnly;
				}
				ignore = txt.Text.Split(';');
				return dlg.FileName;
			} else
				return null;
		}
		
		private void Execute_Click(object sender, RoutedEventArgs e)
		{
			var tag = ((MenuItem)sender).Tag.ToString();
			var ok = false;
			switch (tag) {
				case "1":
					var dp = getDirectoryPath("bmp");
					if (Directory.Exists(dp)) {
						ok = true;
						filelist.Clear();
						//ファイル名にignoreが含まれていないものを追加する
						filelist.AddRange(Directory.EnumerateFiles(dp, "*.bmp", so)
						.Where(f => ignore.All(n => !Path.GetFileName(f).Contains(n))));
					}
					break;
				case "2":
					dp = getDirectoryPath("jpg");
					if (Directory.Exists(dp)) {
						ok = true;
						filelist.Clear();
						//ファイル名にignoreが含まれていないものを追加する
						filelist.AddRange(Directory.EnumerateFiles(dp, "*.jpg", so)
						.Where(f => ignore.All(n => !Path.GetFileName(f).Contains(n))));
					}
					break;
				case "3":
					var dlg = new CommonOpenFileDialog();
					dlg.Title = "List";
					dlg.Filters.Add(new CommonFileDialogFilter("FileList", "*.txt,*.log,*.csv")); 
					if (dlg.ShowDialog() == CommonFileDialogResult.Ok) {
						ok = true;
						filelist.Clear();
						filelist.AddRange(File.ReadAllLines(dlg.FileName));
					}
					break;
			}
			if (ok) {
				pbar.Minimum = 0;
				pbar.Maximum = filelist.Count();
				i = 0;
				Retry = 0;
				comp.Clear();
				RecursiveFunction();
			}
		}
		
		private void Resume_Click(object sender, RoutedEventArgs e)
		{
			suspend = false;
			RecursiveFunction();
		}
		
		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			suspend = false;
			label.Content = "";
			pbar.Value = 0;
			menufile.Visibility = Visibility.Visible;
			menuoption.Visibility = Visibility.Hidden;
		}
		
		private void UserControl_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape) {
				if (i == filelist.Count()) {
					this.Close();
				} else if (!suspend) {
					label.Content += " [Suspending....]";
					suspend = true;
				}
			}
		}
	}
}
