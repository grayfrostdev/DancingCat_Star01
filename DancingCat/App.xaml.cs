using System;
using System.Windows;

namespace DancingCat
{
    public partial class App : System.Windows.Application
    {
        private TrayIconManager? _trayIconManager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 앱이 시작될 때 트레이 아이콘을 생성합니다.
            _trayIconManager = new TrayIconManager();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 앱이 종료될 때 트레이 아이콘 리소스를 해제합니다.
            _trayIconManager?.Dispose();
            base.OnExit(e);
        }
    }
}
