using System;
using System.IO;
using System.Windows.Controls;
using System.Windows;

namespace Hcode
{
    public partial class ShortCutWindow : Window
    {
        // 인자로 받아온 윈도우와 선택된 언어
        private Window window;
        private string selectLanguage;

        // Hcode 폴더 경로 불러오기
        static string folderPath = "C:/Hcode";
        static string projectPath = folderPath + "/Project";

        public ShortCutWindow(Window window, string Language)
        {
            InitializeComponent();

            this.window = window;
            this.selectLanguage = Language;
            SelectedLabel.Content = Language;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // 프로젝트명 가져오기
            string fileName = FileName_TextBox.Text;
            DirectoryInfo newProjectInfoPath = new DirectoryInfo(projectPath + "/" + fileName);
            FileInfo newFilePath;

            // 선택된 언어에 따른 파일 확장자 설정
            switch (selectLanguage.ToLower())
            {
                case "c":
                    newFilePath = new FileInfo(projectPath + "/" + fileName + "/" + fileName + ".c");
                    break;
                case "java":
                    newFilePath = new FileInfo(projectPath + "/" + fileName + "/" + fileName + ".java");
                    break;
                case "python":
                    newFilePath = new FileInfo(projectPath + "/" + fileName + "/" + fileName + ".py");
                    break;
                default:
                    MessageBox.Show("지원되지 않는 언어입니다.");
                    return;
            }

            // 해당 프로젝트 폴더 유무 체크 후 없을 시 생성
            if (!newProjectInfoPath.Exists)
            {
                newProjectInfoPath.Create();
                newFilePath.Create();
            }

            // 선택된 언어에 따른 Window 열기
            Window newWindow;
            switch (selectLanguage.ToLower())
            {
                case "c":
                    newWindow = new CWindow(selectLanguage, fileName);
                    break;
                case "java":
                    newWindow = new JavaWindow(selectLanguage, fileName);
                    break;
                case "python":
                    newWindow = new PythonWindow(selectLanguage, fileName);
                    break;
                default:
                    MessageBox.Show("지원되지 않는 언어입니다.");
                    return;
            }

            newWindow.Show();
            window.Close();
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FileName_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!okButton.IsEnabled && FileName_TextBox.Text.Length > 0)
                okButton.IsEnabled = true;
            else if (okButton.IsEnabled && FileName_TextBox.Text.Length <= 0)
                okButton.IsEnabled = false;
        }
    }
}
