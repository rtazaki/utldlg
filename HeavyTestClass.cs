using System;
using System.Threading;
namespace utldlg
{
	/// <summary>
	/// Description of HeavyTestClass.
	/// </summary>
	public sealed class HeavyTestClass
	{
		private static HeavyTestClass instance = new HeavyTestClass();
		
		public static HeavyTestClass Instance {
			get {
				return instance;
			}
		}
		
		private HeavyTestClass()
		{
		}
		
		public void TestCase1(string data, CancellationTokenSource tokenSource)
		{
			Thread.Sleep(500);
			while (true) {
				if (data == "2") {
					throw new InvalidOperationException();
				} else if (data == "3") {
					throw new Exception();
				} else if (data == "4") {
					tokenSource.Token.ThrowIfCancellationRequested();
				} else if (data == "5") {
					//暴走
				} else {
					break;
				}
			}
		}
	}
}
