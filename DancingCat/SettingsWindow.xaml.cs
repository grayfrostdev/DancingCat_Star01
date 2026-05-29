using System.Windows;

namespace DancingCat
{
    public partial class SettingsWindow : Window
    {
        private bool _isInitializing = false;

        public SettingsWindow()
        {
            _isInitializing = true;
            InitializeComponent();

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                SensitivitySlider.Value = mainWindow.SpeedSensitivity;
                SizeSlider.Value = mainWindow.CatSize;
                ShowStatusCheckBox.IsChecked = mainWindow.ShowStatusText;
                
                if (mainWindow.SelectedCatType == 1) Cat1Radio.IsChecked = true;
                else if (mainWindow.SelectedCatType == 2) Cat2Radio.IsChecked = true;
                else if (mainWindow.SelectedCatType == 3) Cat3Radio.IsChecked = true;
                else if (mainWindow.SelectedCatType == 4) Cat4Radio.IsChecked = true;
                else if (mainWindow.SelectedCatType == 5) Cat5Radio.IsChecked = true;
                else if (mainWindow.SelectedCatType == 6) Cat6Radio.IsChecked = true;
            }
            
            _isInitializing = false;
        }

        private void ApplySettings()
        {
            if (_isInitializing) return;

            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                int catType = 1;
                if (Cat2Radio.IsChecked == true) catType = 2;
                else if (Cat3Radio.IsChecked == true) catType = 3;
                else if (Cat4Radio.IsChecked == true) catType = 4;
                else if (Cat5Radio.IsChecked == true) catType = 5;
                else if (Cat6Radio.IsChecked == true) catType = 6;

                mainWindow.SetSettings(SensitivitySlider.Value, SizeSlider.Value, ShowStatusCheckBox.IsChecked ?? true, catType);
            }
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplySettings();
        }

        private void OnCheckChanged(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private void ChangePositionButton_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.EnterMoveMode();
            }
            this.Close(); // 위치 변경 모드에 진입하면 설정 창은 닫습니다.
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
