using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;

namespace DancingCat
{
    public class InputHookManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private IntPtr _keyboardHookID = IntPtr.Zero;
        private IntPtr _mouseHookID = IntPtr.Zero;
        private LowLevelProc _keyboardProc;
        private LowLevelProc _mouseProc;

        private int _activityScore = 0;
        private DispatcherTimer _activityTimer;
        private Queue<int> _scoreHistory = new Queue<int>();

        // 초당 계산된 활성도 점수를 전달하는 이벤트
        public event EventHandler<int>? OnActivityLevelUpdated;

        public InputHookManager()
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            _keyboardHookID = SetHook(WH_KEYBOARD_LL, _keyboardProc);
            _mouseHookID = SetHook(WH_MOUSE_LL, _mouseProc);

            // 0.5초마다 활성도를 계산하여 전달하는 타이머
            _activityTimer = new DispatcherTimer();
            _activityTimer.Interval = TimeSpan.FromMilliseconds(500);
            _activityTimer.Tick += ActivityTimer_Tick;
            _activityTimer.Start();
        }

        private void ActivityTimer_Tick(object? sender, EventArgs e)
        {
            _scoreHistory.Enqueue(_activityScore);
            _activityScore = 0; // 점수 초기화
            
            // 3틱(1.5초) 유지
            if (_scoreHistory.Count > 3)
            {
                _scoreHistory.Dequeue();
            }

            // 최근 3틱 점수 합산
            int totalScore = _scoreHistory.Sum();
            
            OnActivityLevelUpdated?.Invoke(this, totalScore);
        }

        private IntPtr SetHook(int idHook, LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                _activityScore += 10; // 키보드 입력은 가중치 10
            }
            // 이벤트를 가로채지 않고 다음 프로그램(OS)으로 넘김
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_MOUSEMOVE)
                {
                    _activityScore += 1; // 마우스 이동은 매우 빈번하므로 가중치 1
                }
                else if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN)
                {
                    _activityScore += 10; // 클릭은 가중치 10
                }
            }
            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            _activityTimer.Stop();
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }
    }
}
