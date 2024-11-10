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
    public partial class CWindow : Window
    {
        // Hcode 폴더 경로 불러오기
        static string folderPath = "C:/Hcode";
        static string projectPath = folderPath + "/Project";
        static string userPath = folderPath + "/UserSetting";

        string userProjectPath;

        DirectoryInfo projectInfoPath = new DirectoryInfo(projectPath);
        DirectoryInfo userInfoPath = new DirectoryInfo(userPath);

        public CWindow(string selectLanguage, string fileName)
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
            } catch (UnauthorizedAccessException)
            {
                // 접근 권한이 없는 폴더는 무시
            } catch (Exception ex)
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
            // 사용자가 입력한 파일명을 가져옵니다.
            string fileName = FileName_Label.Content.ToString();

            // 파일명이 null이거나 빈 문자열인지 확인합니다.
            if (string.IsNullOrEmpty(fileName))
            {
                OutputTextBox.Text = "에러: 파일명을 입력해주세요.";
                return;
            }

            // 파일 내용을 가져옵니다.
            string sourceCode = CodeTextBox.Text;

            // 파일 경로
            string cFile = Path.Combine(userProjectPath, $"{fileName}.c");

            try
            {
                // 코드를 .c 파일로 저장합니다.
                File.WriteAllText(cFile, sourceCode);

                // Define input values
                string input = "5"; // Example input value

                // 컴파일된 프로그램이 저장될 .exe 파일 경로
                string exeFile = Path.Combine(userProjectPath, $"{fileName}.exe");

                // 컴파일러 실행
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\LLVM\bin\clang.exe",
                    Arguments = $"-target x86_64-pc-windows-msvc {cFile} -o \"{exeFile}\"",
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
                        // Pass input to the process
                        using (StreamWriter sw = process.StandardInput)
                        {
                            if (sw.BaseStream.CanWrite)
                            {
                                // Write input to the standard input stream
                                sw.WriteLine(input);
                                sw.Flush();
                            }
                        }

                        // 컴파일러에서 오류 메시지 가져오기
                        StringBuilder error = new StringBuilder();
                        process.ErrorDataReceived += (s, args) => error.AppendLine(args.Data);
                        process.BeginErrorReadLine();

                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            // 컴파일이 성공 → 컴파일된 프로그램 실행
                            ProcessStartInfo psi2 = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/k echo off & echo \"{userProjectPath}\" && \"{exeFile}\"",
                                UseShellExecute = false,
                                CreateNoWindow = false
                            };

                            Process.Start(psi2);
                        }
                        else
                        {
                            // 컴파일 에러 발생 시 OutputTextBox에 에러 메시지 출력
                            OutputTextBox.Text = "컴파일 에러";
                            OutputTextBox.AppendText(TranslateErrorMessage(error.ToString()));                        }
                    }
                }
            } catch (Exception ex)
            {
                // 파일 저장 중 오류가 발생한 경우 오류 메시지를 표시합니다.
                OutputTextBox.Text = $"파일 저장 중 오류가 발생했습니다: {ex.Message}";
            }
            //트리 뷰 새로고침
            LoadFolderStructure(folderPath);
        }

        
        // 에러 메세지 한글 하드코딩
        private string TranslateErrorMessage(string errorMessage)
        {
            var translations = new Dictionary<string, string>
            {
                { @"error:", "에러:" },
                { @"warning:", "경고:" },
                { @"1 error generated", "1개의 에러가 발생했습니다" },

            // 구문 오류
            { @"expected ';' after expression", "표현식 뒤에 ';'가 필요합니다" },
            { @"expected ';' at end of declaration", "선언문의 끝에 ';'가 필요합니다" },
            { @"expected identifier or '\('", "식별자 또는 '('이(가) 필요합니다" },
            { @"expected unqualified-id", "자격이 없는 ID가 필요합니다" },
            { @"expected expression", "표현식이 필요합니다" },
            { @"expected '\)', got ';'", "');'이 필요합니다, ';'를 발견했습니다" },
            { @"expected parameter declarator", "매개변수 선언자가 필요합니다" },
            { @"expected '\}'", "'}'이(가) 필요합니다" },
            { @"expected '>'", "'>'이(가) 필요합니다" },
            { @"expected declaration", "선언이 필요합니다" },

            // 타입 오류
            { @"cannot convert '(.*)' to '(.*)' in initialization", "초기화에서 '{0}'를 '{1}'로 변환할 수 없습니다" },
            { @"incompatible type for argument", "인수에 대해 호환되지 않는 타입입니다" },
            { @"incompatible pointer to integer conversion passing '(.*)' to parameter of type '(.*)'", "'{0}'를 타입 '{1}'의 매개변수로 전달하는 포인터에서 정수로의 호환되지 않는 변환" },
            { @"return type does not match the function type", "반환 타입이 함수 타입과 일치하지 않습니다" },

            // 선언되지 않음 또는 정의되지 않음
            { @"use of undeclared identifier '(.*)'", "선언되지 않은 식별자 '{0}'의 사용" },
            { @"undefined reference to '(.*)'", "'{0}'에 대한 정의되지 않은 참조" },
            { @"implicit declaration of function '(.*)' is invalid in C99", "함수 '{0}'의 암시적 선언은 C99에서 유효하지 않습니다" },

            // 함수 및 변수 오류
            { @"no matching function for call to '(.*)'", "'{0}' 호출에 대한 일치하는 함수가 없습니다" },
            { @"too few arguments to function call", "함수 호출에 인수가 너무 적습니다" },
            { @"too many arguments to function call", "함수 호출에 인수가 너무 많습니다" },
            { @"redefinition of '(.*)'", "'{0}'의 재정의" },
            { @"conflicting types for '(.*)'", "'{0}'의 타입 충돌" },
            { @"function '(.*)' is not defined", "함수 '{0}'이(가) 정의되지 않았습니다" },

            // 범위 및 접근 오류
            { @"invalid use of non-static member function", "비정적 멤버 함수의 잘못된 사용" },
            { @"invalid use of 'this' in non-member function", "비멤버 함수에서 'this'의 잘못된 사용" },
            { @"member access into incomplete type '(.*)'", "불완전한 타입 '{0}'에 대한 멤버 접근" },
            { @"no member named '(.*)' in '(.*)'", "'{1}'에 '{0}'라는 멤버가 없습니다" },

            // 포인터 및 배열 오류
            { @"subscripted value is not an array, pointer, or vector", "서브스크립트된 값이 배열, 포인터 또는 벡터가 아닙니다" },
            { @"dereferencing pointer to incomplete type", "불완전한 타입에 대한 포인터 역참조" },
            { @"array subscript is not an integer", "배열 서브스크립트가 정수가 아닙니다" },
            { @"invalid operands to binary expression", "이진 표현식에 대한 잘못된 피연산자" },

            // 링커 오류
            { @"multiple definition of '(.*)'", "'{0}'의 다중 정의" },
            { @"cannot find -l(.*)", "-l{0}을(를) 찾을 수 없습니다" },

            // 경고
            { @"comparison between signed and unsigned integer expressions", "부호가 있는 정수 표현식과 부호가 없는 정수 표현식 간의 비교" },
            { @"unused variable '(.*)'", "사용되지 않은 변수 '{0}'" },
            { @"unused function '(.*)'", "사용되지 않은 함수 '{0}'" },
            { @"control reaches end of non-void function", "non-void 함수의 끝에 도달합니다" }
            };

            foreach (var entry in translations)
            {
                var regex = new Regex(entry.Key);
                var matches = regex.Matches(errorMessage);
                foreach (Match match in matches)
                // 오류에서 사용자 입력값 *를 찾아내기 위해 캡처그룹
                {
                    var groups = new List<string>();
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        groups.Add(match.Groups[i].Value);
                    }
                    errorMessage = regex.Replace(errorMessage, string.Format(entry.Value, groups.ToArray()));
                }
            }
            return errorMessage;
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
                string cFile = userProjectPath + "/" + FileName_Label.Content.ToString() + ".c";

                // 파일을 저장합니다.
                File.WriteAllText(cFile, code);

                // 성공적으로 저장되었다는 메시지를 표시합니다.
                MessageBox.Show("파일이 성공적으로 저장되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex)
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