using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace romajihud
{
	[Flags]
	enum ModifierKeys : uint
	{
		Alt = 1,
		Control = 2,
		Shift = 4,
		Win = 8
	}

	class KeyPressedEventArgs : EventArgs
	{
		public ModifierKeys Modifier;
		public Keys Key;

		public KeyPressedEventArgs(ModifierKeys modifier, Keys key)
		{
			this.Modifier = modifier;
			this.Key = key;
		}
	}

	class Window : NativeWindow, IDisposable
	{
		private static int WM_HOTKEY = 0x0312;
		public event EventHandler<KeyPressedEventArgs> KeyPressed = (s, e) => { };

		public Window()
		{
			CreateHandle(new CreateParams());
		}

		protected override void WndProc(ref Message msg)
		{
			base.WndProc(ref msg);

			if (msg.Msg == WM_HOTKEY)
			{
				Keys key = (Keys)(((int)msg.LParam >> 16) & 0xFFFF);
				ModifierKeys modifier = (ModifierKeys)((int)msg.LParam & 0xFFFF);

				KeyPressed(this, new KeyPressedEventArgs(modifier, key));
			}
		}

		public void Dispose()
		{
			DestroyHandle();
		}
	}

	class GlobalHotkey
	{
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private Window window = new Window();
		private int currentId;

		public event EventHandler<KeyPressedEventArgs> KeyPressed = (s, e) => { };

		public GlobalHotkey()
		{
			window.KeyPressed += (s, e) => KeyPressed(this, e);
		}

		public void RegisterHotkey(ModifierKeys modifier, Keys key)
		{
			currentId = currentId + 1;

			if (!RegisterHotKey(window.Handle, currentId, (uint)modifier, (uint)key))
				throw new InvalidOperationException("Couldn’t register the hot key.");
		}

		public void Dispose()
		{
			for (int i = currentId; i > 0; i--)
				UnregisterHotKey(window.Handle, i);
			
			window.Dispose();
		}
	}
}
