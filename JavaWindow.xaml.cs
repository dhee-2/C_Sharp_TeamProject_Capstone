using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Windows.Documents;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;

namespace Hcode
{
    public partial class JavaWindow : Window
    {
        // Hcode 폴더 경로 불러오기
        static string folderPath = "C:/Hcode";
        static string projectPath = folderPath + "/Project";
        static string userPath = folderPath + "/UserSetting";

        string userProjectPath;

        DirectoryInfo projectInfoPath = new DirectoryInfo(projectPath);
        DirectoryInfo userInfoPath = new DirectoryInfo(userPath);

        public JavaWindow(string selectLanguage, string fileName)
        {
            this.InitializeComponent();

            //파일명 라벨 설정 및 UserProject 경로 지정
            FileName_Label.Content = fileName;
            userProjectPath = projectPath + "/" + fileName;

            //트리 형식 구성
            LoadFolderStructure(folderPath);

            //작업 표시줄에 맞춰 최소/대 사이즈 조정
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            this.MinHeight = 900;
            this.MaxHeight = SystemParameters.WorkArea.Height;

            // CodeTextBox의 TextChanged 이벤트에 CodeTextBox_TextChanged 메서드를 연결
            this.MouseLeftButtonDown += new MouseButtonEventHandler(MainWindow_MouseLeftButtonDown);
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void LoadFolderStructure(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show("폴더를 찾을 수 없습니다.");
                return;
            }

            FolderItem rootFolder = new FolderItem(Path.GetFileName(rootPath));
            LoadSubItems(rootPath, rootFolder);

            folderTreeView.ItemsSource = new ObservableCollection<FolderItem> { rootFolder };
        }

        private void LoadSubItems(string folderPath, FolderItem parentFolder)
        {
            try
            {
                string[] subDirectories = Directory.GetDirectories(folderPath);
                foreach (string subDir in subDirectories)
                {
                    FolderItem subFolder = new FolderItem(Path.GetFileName(subDir));
                    subFolder.Icon = "Resources/folder.png";
                    parentFolder.SubItems.Add(subFolder);
                    LoadSubItems(subDir, subFolder); // 하위 폴더 로드
                }

                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    FileItem fileItem = new FileItem(Path.GetFileName(file));
                    fileItem.Icon = "Resources/cFile.png";
                    parentFolder.SubItems.Add(fileItem);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 접근 권한이 없는 폴더는 무시
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}");
            }
        }

        private void ItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            String itemPath = projectPath + "/" + Path.GetFileNameWithoutExtension(textBlock.Text);
            if (textBlock == null || !(textBlock.Text.Split('.')[textBlock.Text.Split('.').Length - 1].Equals("c")))
                return;

            // 이전 파일을 저장합니다.
            string cFile = userProjectPath + "/" + FileName_Label.Content.ToString() + ".c";
            File.WriteAllText(cFile, CodeTextBox.Text);

            FileName_Label.Content = textBlock.Text.Split('.')[0];
            CodeTextBox.Text = File.ReadAllText(itemPath + "/" + textBlock.Text);

            userProjectPath = projectPath + "/" + Path.GetFileNameWithoutExtension(textBlock.Text);
        }

        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (folderTreeView.SelectedItem != null && folderTreeView.SelectedItem is FileItem)
            {
                FileItem selectedItem = (FileItem)folderTreeView.SelectedItem;
                string filePath = Path.Combine("C:\\Your\\Specific\\Path", selectedItem.Name); // 파일 경로 설정
                Process.Start(filePath); // 파일 열기
            }
        }

        private void CloseFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 파일을 닫는 추가적인 동작이 필요한 경우 이곳에 구현합니다.
        }

        private void OpenFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (folderTreeView.SelectedItem != null && folderTreeView.SelectedItem is FolderItem)
            {
                FolderItem selectedItem = (FolderItem)folderTreeView.SelectedItem;
                string folderPath = Path.Combine("C:\\Your\\Specific\\Path", selectedItem.Name); // 폴더 경로 설정
                Process.Start(folderPath); // 폴더 열기
            }
        }

        private void CloseFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 폴더를 닫는 추가적인 동작이 필요한 경우 이곳에 구현합니다.
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = FileName_Label.Content.ToString();

            if (string.IsNullOrEmpty(fileName))
            {
                OutputTextBox.Text = "에러: 파일명을 입력해주세요.";
                return;
            }

            string sourceCode = CodeTextBox.Text;
            string javaFilePath = Path.Combine(userProjectPath, $"{fileName}.java");
            string classFilePath = Path.Combine(userProjectPath, fileName);

            try
            {
                // 소스 코드 파일 저장
                File.WriteAllText(javaFilePath, sourceCode);

                // 자바 파일 컴파일
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\Java\jdk-22\bin\javac.exe",
                    Arguments = $"-d \"{userProjectPath}\" \"{javaFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        StringBuilder error = new StringBuilder();
                        process.ErrorDataReceived += (s, args) => error.AppendLine(args.Data);
                        process.BeginErrorReadLine();

                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            OutputTextBox.Text = "컴파일 에러";
                            OutputTextBox.AppendText("\n" + error.ToString());
                            return;
                        }
                    }
                }

                // 컴파일된 클래스 파일 실행
                ProcessStartInfo psi2 = new ProcessStartInfo
                {
                    FileName = @"C:\Windows\System32\cmd.exe",
                    Arguments = $"/k java -cp \"{userProjectPath}\" {fileName}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(psi2);
            }
            catch (Exception ex)
            {
                OutputTextBox.Text = $"파일 저장 중 오류가 발생했습니다: {ex.Message}";
            }
        }




        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            // 파일을 열기 위한 OpenFileDialog 호출
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                String fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                if (fileName.Equals(FileName_Label.Content.ToString()))
                    return;
                else
                {
                    FileName_Label.Content = openFileDialog.FileName;
                    CodeTextBox.Text = File.ReadAllText(openFileDialog.FileName);

                    userProjectPath = projectPath + "/" + Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                }
            }
        }

        private void TextScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                HighlightScrollViewer.ScrollToVerticalOffset(TextScrollViewer.VerticalOffset);
            }
        }


        private bool isTextChanging = false;

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;

            if (isTextChanging)
                return;

            isTextChanging = true;

            string text = textBox.Text;
            HighlightedTextBlock.Inlines.Clear();

            var patterns = new Dictionary<string, Brush>
    {
         { @"#define", new SolidColorBrush(Color.FromRgb(61, 189, 61)) }, // 진한 초록색
        { @"#include", new SolidColorBrush(Color.FromRgb(61, 189, 61)) },

        { @"\bprintf\b", new SolidColorBrush(Color.FromRgb(255, 165, 0)) }, // 연한 주황색

        { @"\bif\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) }, // 연한 분홍색
        { @"\belse\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bwhile\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bfor\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bswitch\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bcase\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bbreak\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bcontinue\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bdo\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },
        { @"\bgoto\b", new SolidColorBrush(Color.FromRgb(243, 176, 195)) },

        { @"\bint\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) }, // 파랑
        { @"\bconst\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bstatic\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bextern\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bstruct\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bunion\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\btypedef\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\benum\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bsizeof\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bchar\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bdouble\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bfloat\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bvoid\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bshort\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\blong\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) },
        { @"\bunsigned\b", new SolidColorBrush(Color.FromRgb(25, 153, 228)) }
    };

            int lastIndex = 0;
            var allMatches = new List<Match>();
            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(text, pattern.Key);
                allMatches.AddRange(matches.Cast<Match>());
            }

            allMatches = allMatches.OrderBy(m => m.Index).ToList();
            foreach (var match in allMatches)
            {
                if (match.Index > lastIndex)
                {
                    HighlightedTextBlock.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
                }

                Run run = new Run(match.Value);
                run.Foreground = patterns.First(p => Regex.IsMatch(match.Value, p.Key)).Value;
                HighlightedTextBlock.Inlines.Add(run);

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                HighlightedTextBlock.Inlines.Add(new Run(text.Substring(lastIndex)));
            }

            isTextChanging = false;

            foreach (TextChange change in e.Changes)
            {
                int offset = change.Offset;
                int addedLength = change.AddedLength;
                string addedText = textBox.Text.Substring(offset, addedLength);
                int caretIndex2 = textBox.CaretIndex;

                // 새로 추가된 문자가 괄호나 따옴표인지 확인합니다.
                if (addedText == "{" || addedText == "(" || addedText == "[" || addedText == "'" || addedText == "\"")
                {
                    // 현재 커서 위치를 저장합니다.
                    int caretIndex = textBox.CaretIndex;
                    // 추가된 문자가 여는 괄호나 따옴표일 경우에는 그에 해당하는 닫는 괄호나 따옴표를 추가하고 커서를 원래 위치로 되돌립니다.
                    string closingBracket = addedText == "{" ? "}" : (addedText == "(" ? ")" : (addedText == "[" ? "]" : (addedText == "'" ? "'" : "\"")));
                    if (offset + addedLength < textBox.Text.Length && textBox.Text[offset + addedLength] == closingBracket[0])
                    {
                        // 이미 같은 종류의 괄호가 입력된 경우에는 추가하지 않습니다.
                        textBox.CaretIndex = caretIndex;
                    }
                    else
                    {
                        // 다음에 추가될 문자열을 계산합니다.
                        string nextChar = (offset + addedLength < textBox.Text.Length) ? textBox.Text[offset + addedLength].ToString() : "";
                        // 만약 다음 문자가 같은 종류의 괄호나 따옴표이면 추가하지 않습니다.
                        if (closingBracket == nextChar)
                        {
                            textBox.CaretIndex = caretIndex;
                        }
                        else
                        {
                            // 아니면 추가합니다.
                            textBox.Text = textBox.Text.Insert(offset + addedLength, closingBracket);
                            textBox.CaretIndex = caretIndex;
                        }
                    }
                }
            }
        }

        private void CodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;

            if (e.Key == Key.Tab)
            {
                InsertTab(textBox, 1);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                int caretIndex = textBox.CaretIndex;
                string text = textBox.Text;
                int openBraceIndex = text.LastIndexOf('{', caretIndex - 1);
                int closeBraceIndex = text.LastIndexOf('}', caretIndex - 1);

                // 중괄호 내부에 있는지 확인
                if (openBraceIndex > closeBraceIndex)
                {
                    int indentLevel = GetIndentLevel(text, caretIndex);

                    // Insert two new lines
                    textBox.Text = text.Insert(caretIndex, Environment.NewLine + Environment.NewLine);
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;

                    // 첫 번째 새로운 줄 다음에 Tab 입력
                    InsertTab(textBox, indentLevel);

                    // 두 번째 새로운 줄에 올바른 들여쓰기 적용
                    int nextLineStartIndex = textBox.CaretIndex + Environment.NewLine.Length;
                    if (nextLineStartIndex < textBox.Text.Length && textBox.Text[nextLineStartIndex] == '}')
                    {
                        textBox.CaretIndex += Environment.NewLine.Length;
                        InsertTab(textBox, indentLevel - 1);
                    }

                    e.Handled = true;
                }
            }
        }

        private void InsertTab(TextBox textBox, int tabCount)
        {
            if (textBox != null)
            {
                int caretIndex = textBox.CaretIndex;
                string tabs = new string('\t', tabCount);
                textBox.Text = textBox.Text.Insert(caretIndex, tabs);
                textBox.CaretIndex = caretIndex + tabCount;
            }
        }

        private int GetIndentLevel(string text, int caretIndex)
        {
            int openBraceCount = 0;
            int closeBraceCount = 0;

            for (int i = 0; i < caretIndex; i++)
            {
                if (text[i] == '{')
                {
                    openBraceCount++;
                }
                else if (text[i] == '}')
                {
                    closeBraceCount++;
                }
            }

            // 중첩된 수준을 반환
            return openBraceCount - closeBraceCount;
        }




        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 파일 내용을 가져옵니다.
            string code = CodeTextBox.Text;

            try
            {
                // 파일을 저장할 경로를 지정합니다.
                string cFile = userProjectPath + "/" + FileName_Label.Content.ToString() + ".java";

                // 파일을 저장합니다.
                File.WriteAllText(cFile, code);

                // 성공적으로 저장되었다는 메시지를 표시합니다.
                MessageBox.Show("파일이 성공적으로 저장되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 파일 저장 중 오류가 발생한 경우 오류 메시지를 표시합니다.
                MessageBox.Show($"파일 저장 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //트리 뷰 새로고침
            LoadFolderStructure(folderPath);
        }

        private void OnClickCButton(object sender, RoutedEventArgs e)
        {
            Window newWindow = new ShortCutWindow(this, "C");
            newWindow.Show();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Window HelpWindow = new HelpWindow();
            HelpWindow.Show();
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {

        }

        // 클랭-포맷을 활용한 들여쓰기
        private void IndentButton_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeTextBox.Text;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\LLVM\bin\clang-format.exe",
                Arguments = "-style=LLVM",
                /*
                  Google 스타일: "-style=google"
                  LLVM 스타일: "-style=llvm"
                  GNU 스타일: "-style=gnu"
                  Chromium 스타일: "-style=chromium"
                  file: "-style=file"
                  custom: "-style={key: value, key: value, ...}"
                  fallback: "-style=fallback"
                  none: "-style=none"
                  */
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                if (process != null)
                {
                    using (StreamWriter sw = process.StandardInput)
                    {
                        if (sw.BaseStream.CanWrite)
                        {
                            // 코드를 입력 스트림으로 전달
                            sw.WriteLine(code);
                            sw.Flush();
                        }
                    }

                    // clang-format에서 처리된 코드를 읽어옵니다.
                    string formattedCode = process.StandardOutput.ReadToEnd();

                    // clang-format 프로세스 종료 대기
                    process.WaitForExit();

                    // 들여쓴 코드를 텍스트 상자에 설정합니다.
                    CodeTextBox.Text = formattedCode;
                }
            }
        }

        private void ExplorerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = ExplorerWindow.Width + e.HorizontalChange;

            if (newWidth > 100 && newWidth < 1800)
            {
                ExplorerWindow.Width = newWidth;
            }
        }

        private void EditorThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = TextBoxBorder.Height + e.VerticalChange;

            if (newHeight > 100 && newHeight < 900)
            {
                TextBoxBorder.Height = newHeight;
            }
        }



        private void ToMiniButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void SizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.ResizeMode = ResizeMode.CanResize;
                this.WindowState = WindowState.Normal;
                SizeButton.Content = "1";
                ShadowBorder.Margin = new Thickness(5);
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.ResizeMode = ResizeMode.NoResize;
                this.WindowState = WindowState.Maximized;
                SizeButton.Content = "2";
                ShadowBorder.Margin = new Thickness(0);
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}