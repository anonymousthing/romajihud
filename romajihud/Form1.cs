using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace romajihud
{
	public partial class Form1 : Form
	{
		GlobalHotkey hotkey = new GlobalHotkey();
		bool _visible = false;
		bool ActuallyVisible
		{
			get { return _visible && this.Visible; }
			set { _visible = value; Visible = value; }
		}

		public Form1()
		{
			InitializeComponent();
			hotkey.RegisterHotkey(romajihud.ModifierKeys.Control | romajihud.ModifierKeys.Alt, Keys.F12);
			hotkey.KeyPressed += Hotkey_KeyPressed;
			SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override void SetVisibleCore(bool value)
		{
			base.SetVisibleCore(_visible && value);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
		}

		private void Hotkey_KeyPressed(object sender, KeyPressedEventArgs e)
		{
			if (!Clipboard.ContainsText())
				return;

			string text = Clipboard.GetText();
			//string preprocessed = Preprocess(text);
			string converted = ConvertToRomaji(text);
			ShowHud(converted);
		}

		async void ShowHud(string text)
		{
			this.ActuallyVisible = true;
			this.TopMost = true;
			this.Location = new Point(Cursor.Position.X, Cursor.Position.Y - this.Height);
			label1.Text = text;
			var stringWidth = this.CreateGraphics().MeasureString(text, label1.Font).Width;
			this.Width = (int)stringWidth + 10;
			await Task.Run(() =>
			{
				Thread.Sleep(3000);
			});
			this.TopMost = false;
			this.ActuallyVisible = false;
		}

		//Strip out everything other than letters and numbers and change to lowercase
		string Preprocess(string input)
		{
			List<char> result = new List<char>();
			for (int i = 0; i < input.Length; i++)
			{
				if ((input[i] >= '0' && input[i] <= '9') || (input[i] >= 'a' && input[i] <= 'z'))
				{
					result.Add(input[i]);
				}
				else if (input[i] >= 'A' && input[i] <= 'Z')
				{
					result.Add((char)(input[i] | 32));
				}
			}

			return string.Join("", result);
		}

		//Process the ^ and {|} cases, and strip out all other characters other than letters and numbers. Also convert to lowercase.
		string Postprocess(string input)
		{
			List<char> newString = new List<char>();
			for (int i = 0; i < input.Length; i++)
			{
				bool lastSpace = false;
				if (input[i] == '^' && i > 0)
				{
					switch (input[i - 1])
					{
						case 'a':
							newString.Add('a');
							break;
						case 'e':
							newString.Add('e');
							break;
						case 'o':
							newString.Add('u');
							break;

					}
				}
				else if (input[i] == '{' || input[i] == '}' || input[i] == '|' ||
					(input[i] >= '0' && input[i] <= '9') || (input[i] >= 'a' && input[i] <= 'z'))
				{
					lastSpace = false;
					newString.Add(input[i]);
				}
				else if (input[i] >= 'A' && input[i] <= 'Z')
				{
					lastSpace = false;
					newString.Add((char)(input[i] | 32)); //To lower
				}
				else
				{
					if (!lastSpace)
					{
						lastSpace = true;
						newString.Add(' ');
					}
				}
			}

			return string.Join("", newString);
		}

		string ConvertToRomaji(string input)
		{
			Process process = new Process();
			process.StartInfo.FileName = @"C:\kakasi\bin\kakasi.exe";
			process.StartInfo.Arguments = "-s -Ja -Ha -Ka -p -c";
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;

			process.Start();

			StreamWriter writer = new StreamWriter(process.StandardInput.BaseStream, Encoding.GetEncoding(932)); //Codepage 932 = Shift-JIS encoding
			writer.WriteLine(input);
			writer.Flush();
			writer.Close();

			string output = process.StandardOutput.ReadToEnd().Trim();

			return Postprocess(output);
		}
	}
}
