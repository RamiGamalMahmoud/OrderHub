using WindowsInput;
using WindowsInput.Native;

namespace orderu.UI.Helpers
{
    public static class KeyboardSimulator
    {
        private static readonly InputSimulator _simulator = new InputSimulator();

        public static void SelectAll() =>
            _simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);

        public static void InsertNewLine() =>
            _simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.SHIFT, VirtualKeyCode.RETURN);

        public static void SendFind() =>
            _simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_F);

        public static void TypeText(string text)
        {
            if (!string.IsNullOrEmpty(text))
                _simulator.Keyboard.TextEntry(text);
        }

        public static void PressKey(VirtualKeyCode key) =>
            _simulator.Keyboard.KeyPress(key);

        public static void PressEnter() =>
            _simulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);

        public static void PasteText() =>
            _simulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
    }
}
