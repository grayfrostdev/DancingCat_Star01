using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace DancingCat
{
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon? _notifyIcon;

        public TrayIconManager()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Application;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "춤추는 고양이";

            var contextMenu = new ContextMenuStrip();
            
            var settingsItem = new ToolStripMenuItem("설정");
            settingsItem.Click += (s, e) => OpenSettings();
            
            var exitItem = new ToolStripMenuItem("종료");
            exitItem.Click += (s, e) => Application.Current.Shutdown();

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OpenSettings()
        {
            // WPF UI 스레드에서 설정 창 열기 (데드락 방지를 위해 BeginInvoke 사용)
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 이미 열려있는 설정 창이 있는지 확인
                var existingWindow = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
                if (existingWindow != null)
                {
                    // 이미 창이 있다면 최소화 해제 후 활성화(앞으로 가져오기)
                    if (existingWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        existingWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    existingWindow.Activate();
                }
                else
                {
                    // 열려있는 창이 없으면 새로 생성
                    var settingsWindow = new SettingsWindow();
                    settingsWindow.Show();
                }
            }));
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
