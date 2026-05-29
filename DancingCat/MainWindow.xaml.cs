using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;

namespace DancingCat
{
    public partial class MainWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;
        
        private AppSettings _appSettings = new AppSettings();

        // 속도 민감도 (기본값 0.6)
        public double SpeedSensitivity { get; private set; } = 0.6;
        
        // 고양이 크기 (기본값 200)
        public double CatSize { get; private set; } = 200.0;
        
        // 상태 텍스트 표시 여부 (기본값 false)
        public bool ShowStatusText { get; private set; } = false;

        // 선택된 고양이 스킨 타입 (1, 2, 3)
        public int SelectedCatType { get; private set; } = 1;

        // 창의 클릭 무시 여부를 설정하는 메서드
        // isClickThrough가 true이면 마우스 클릭을 무시(통과)하고, false이면 클릭(드래그)을 허용합니다.
        public void SetClickThrough(bool isClickThrough)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (isClickThrough)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }

        // 위치 변경 모드 진입
        public void EnterMoveMode()
        {
            SetClickThrough(false);
            MoveOverlay.Visibility = Visibility.Visible;
            
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        // 위치 변경 모드 종료
        public void ExitMoveMode()
        {
            SetClickThrough(true);
            MoveOverlay.Visibility = Visibility.Collapsed;
            
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        // 오버레이에서 마우스 왼쪽 버튼 클릭 시 드래그 이동
        private void MoveOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // 이동 완료 버튼 클릭
        private void MoveCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            ExitMoveMode();
            SaveCurrentSettings();
            
            // 이동이 끝난 후 설정 창을 다시 띄웁니다.
            var existingWindow = System.Windows.Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            if (existingWindow != null)
            {
                if (existingWindow.WindowState == WindowState.Minimized)
                {
                    existingWindow.WindowState = WindowState.Normal;
                }
                existingWindow.Activate();
            }
            else
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Show();
            }
        }

        // 고양이 이미지를 불러오는 메서드 (타입별 폴더 대응)
        public void LoadCatImages(int catType)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string catFolder = $"Cat{catType}";
            
            // 타입 4, 5, 6은 단일 회전 모드이므로 프레임을 1장만 로드합니다.
            bool isRotationMode = (catType >= 4 && catType <= 6);
            int framesToLoad = isRotationMode ? 1 : TotalFrames;

            // 기존에 남아있을 수 있는 프레임 참조 초기화 (안 쓰는 이미지 RAM 해제용)
            for (int i = 0; i < TotalFrames; i++)
            {
                _frames[i] = null;
            }
            
            for (int i = 0; i < framesToLoad; i++)
            {
                string imagePath = Path.Combine(baseDir, "Images", catFolder, $"frame_{i + 1}.png");
                
                // 해당 폴더에 이미지가 없으면 기존 기본 위치(Images/)에서 불러옵니다.
                if (!File.Exists(imagePath))
                {
                    imagePath = Path.Combine(baseDir, "Images", $"frame_{i + 1}.png");
                }

                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 파일 락 방지
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    _frames[i] = bitmap;
                }
            }

            // 현재 프레임 갱신
            if (_frames[_currentFrame] != null)
            {
                CatImage.Source = _frames[_currentFrame];
            }
        }

        // 설정들을 업데이트하는 메서드
        public void SetSettings(double speedSensitivity, double catSize, bool showStatus, int catType)
        {
            SpeedSensitivity = speedSensitivity;
            
            // 고양이 크기가 변경되었을 때, 왼쪽 아래(좌하단)를 기준으로 하기 위해 Top 위치 보정
            if (CatSize != catSize)
            {
                double heightDiff = catSize - CatSize;
                this.Top -= heightDiff;
            }

            CatSize = catSize;
            ShowStatusText = showStatus;
            
            if (SelectedCatType != catType)
            {
                SelectedCatType = catType;
                _currentFrame = 0; // 스킨 변경 시 첫 프레임으로 초기화
                LoadCatImages(SelectedCatType);
                
                // 이전 스킨의 이미지 데이터들이 차지하던 비관리 메모리를 즉시 반환하도록 GC 강제 실행
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // 회전 각도 초기화
                if (CatImage.RenderTransform is System.Windows.Media.RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = 0;
                }
            }
            
            // 창의 크기 즉시 반영
            this.Width = CatSize;
            this.Height = CatSize;
            
            // 상태 텍스트 가시성 반영
            StatusText.Visibility = ShowStatusText ? Visibility.Visible : Visibility.Hidden;
            
            SaveCurrentSettings();
        }
        
        private void SaveCurrentSettings()
        {
            _appSettings.SpeedSensitivity = SpeedSensitivity;
            _appSettings.CatSize = CatSize;
            _appSettings.ShowStatusText = ShowStatusText;
            _appSettings.SelectedCatType = SelectedCatType;
            _appSettings.WindowLeft = this.Left;
            _appSettings.WindowTop = this.Top;
            _appSettings.Save();
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private InputHookManager? _inputHook;
        private DispatcherTimer _animationTimer;
        private int _currentFrame = 0;
        private const int TotalFrames = 10;
        private BitmapImage[] _frames = new BitmapImage[TotalFrames];

        public MainWindow()
        {
            InitializeComponent();
            
            _appSettings = AppSettings.Load();
            SpeedSensitivity = _appSettings.SpeedSensitivity;
            CatSize = _appSettings.CatSize;
            ShowStatusText = _appSettings.ShowStatusText;
            SelectedCatType = _appSettings.SelectedCatType;
            
            this.Width = CatSize;
            this.Height = CatSize;
            
            // 초기 위치 설정: 가로 중앙, 세로 작업표시줄 바로 위 (WorkArea 기준)
            var workArea = SystemParameters.WorkArea;
            if (double.IsNaN(_appSettings.WindowLeft) || double.IsNaN(_appSettings.WindowTop))
            {
                this.Left = workArea.Left + (workArea.Width - CatSize) / 2;
                this.Top = workArea.Bottom - CatSize;
            }
            else
            {
                this.Left = _appSettings.WindowLeft;
                this.Top = _appSettings.WindowTop;
            }
            
            // 상태 텍스트 초기 가시성 적용
            StatusText.Visibility = ShowStatusText ? Visibility.Visible : Visibility.Hidden;
            
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _animationTimer.Tick += AnimationTimer_Tick;

            // 설정된 고양이 스킨 이미지를 로드합니다.
            LoadCatImages(SelectedCatType);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 초기 상태: 클릭을 통과하도록 설정 (드래그 불가)
            SetClickThrough(true);

            _inputHook = new InputHookManager();
            _inputHook.OnActivityLevelUpdated += InputHook_OnActivityLevelUpdated;

            _animationTimer.Start();
        }

        // 점수를 받아 기본 간격을 계산
        private double CalculateBaseInterval(int score)
        {
            // 속도 민감도를 적용한 점수 계산 (민감도 3.0이 기준 배율 1.0)
            double modifiedScore = score * (SpeedSensitivity / 3.0);

            // 분자 500, fps계산시 분자 2000 기준. (점수 0일때 400ms=5fps)
            return 500.0 / (1.25 + (modifiedScore / 8.0));
        }

        // 애니메이션 타입에 따라 최종 간격을 계산하고 최대/최소 한계 적용
        private double ApplyTypeSpecificLimits(double baseInterval)
        {
            bool isRotationMode = (SelectedCatType >= 4 && SelectedCatType <= 6);
            
            if (isRotationMode)
            {
                // 회전 모드: 간격을 5분의 1로 줄여 프레임을 5배로 늘림
                double targetInterval = baseInterval / 5.0;
                double minInterval = 8.0; // 최고 속도 유지 (0.32초/바퀴)
                double maxInterval = 70.0; // 최소 속도 한계: 한 바퀴 2.8초(70ms * 40틱)
                
                targetInterval = Math.Min(targetInterval, maxInterval);
                return Math.Max(targetInterval, minInterval);
            }
            else
            {
                // 기본 교체 모드
                double targetInterval = baseInterval;
                double minInterval = 40.0; // 최소 40ms 한계 (50fps)
                
                return Math.Max(targetInterval, minInterval);
            }
        }

        // 화면에 표시되는 FPS(프레임 속도) 텍스트를 갱신
        private void UpdateFpsDisplay(double interval)
        {
            double fps = 2000.0 / interval;
            StatusText.Text = $"FPS: {fps:0.0}";
        }

        private void InputHook_OnActivityLevelUpdated(object? sender, int score)
        {
            double baseInterval = CalculateBaseInterval(score);
            double finalInterval = ApplyTypeSpecificLimits(baseInterval);

            var newTimeSpan = TimeSpan.FromMilliseconds(finalInterval);
            if (_animationTimer.Interval != newTimeSpan)
            {
                _animationTimer.Interval = newTimeSpan;
            }
            
            UpdateFpsDisplay(finalInterval);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (SelectedCatType >= 4 && SelectedCatType <= 6)
            {
                // 타입 4, 5, 6: 단일 이미지 회전 처리
                if (CatImage.RenderTransform is System.Windows.Media.RotateTransform rotateTransform)
                {
                    rotateTransform.Angle = (rotateTransform.Angle + 9) % 360;
                }
            }
            else
            {
                // 타입 1, 2, 3: 프레임 교체 애니메이션
                if (CatImage.RenderTransform is System.Windows.Media.RotateTransform rotateTransform)
                {
                    if (rotateTransform.Angle != 0)
                        rotateTransform.Angle = 0;
                }

                _currentFrame = (_currentFrame + 1) % TotalFrames;
                if (_frames[_currentFrame] != null)
                {
                    CatImage.Source = _frames[_currentFrame];
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveCurrentSettings();
            _animationTimer.Stop();
            _inputHook?.Dispose();
            base.OnClosed(e);
        }
    }
}