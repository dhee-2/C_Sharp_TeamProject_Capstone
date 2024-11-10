using System.IO;
using System.Windows;
using System.Windows.Input;
//40차 커밋

namespace Hcode
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Hcode 폴더 경로 불러오기
            string folderPath = "C:/Hcode";
            string projectPath = folderPath + "/Project";
            string userPath = folderPath + "/UserSetting";

            DirectoryInfo projectInfoPath = new DirectoryInfo(projectPath);
            DirectoryInfo userInfoPath = new DirectoryInfo(userPath);

            // 폴더 유/무 체크, 없을 시 생성
            if (!projectInfoPath.Exists)
                projectInfoPath.Create();
            if (!userInfoPath.Exists)
                userInfoPath.Create();

            InitializeComponent();
            this.MouseLeftButtonDown += new MouseButtonEventHandler(MainWindow_MouseLeftButtonDown);
        }
        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // 여러가지 버튼 이벤트들은 모든 컴파일러 및 인터프리터 기능이 구현된 후에 하나의 파일(.xaml, .cs)로 합쳐질 예정
        private void OnClickCButton(object sender, RoutedEventArgs e)
        {
            Window newWindow = new ShortCutWindow(this, "C");
            newWindow.Show();
        }
        private void OnClickJButton(object sender, RoutedEventArgs e)
        {
            Window newWindow = new ShortCutWindow(this, "Java");
            newWindow.Show();
            this.Close();
        }
        private void OnClickPButton(object sender, RoutedEventArgs e)
        {
            Window newWindow = new ShortCutWindow(this, "Python");
            newWindow.Show();
            this.Close();
        }

        private void ToMiniButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
