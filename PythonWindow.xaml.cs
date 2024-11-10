using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Hcode {
    public partial class PythonWindow : Window {
        // 파일 이름
        private string fileName;
        // Hcode 폴더 경로 불러오는곳
        private static string folderPath = "C:/Hcode";
        private static string testPath = folderPath + "/test";
        private static DirectoryInfo directoryInfoPath = new DirectoryInfo(testPath);

        public PythonWindow(string selectLanguage, string fileName) {
            this.fileName = fileName;
            InitializeComponent();
            // CodeTextBox의 TextChanged 이벤트에 CodeTextBox_TextChanged 메서드를 연결
            CodeTextBox.TextChanged += CodeTextBox_TextChanged;
            FileName_Label.Content = fileName;
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e) {
            string sourceCode = CodeTextBox.Text;

            // 소스 코드를 .c 파일로 저장
            string cFile = Path.ChangeExtension(testPath + "/" + fileName, ".c");
            // 컴파일된 프로그램이 저장될 .exe 파일 경로
            string exeFile = testPath + "/" + fileName + ".exe";

            if (fileName == null)
                OutputTextBox.Text = "에러: 파일명을 입력해주세요.";
            else {
                // .c 파일에 소스 코드 작성
                File.WriteAllText(cFile, sourceCode);

                // 컴파일러 실행
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = @"C:\Program Files\LLVM\bin\clang.exe",
                    Arguments = $"-target x86_64-pc-windows-msvc {cFile} -o \"{exeFile}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi)) {
                    if (process != null) {
                        StringBuilder error = new StringBuilder();
                        process.ErrorDataReceived += (s, args) => error.AppendLine(args.Data);
                        process.BeginErrorReadLine();
                        process.WaitForExit();

                        if (process.ExitCode == 0) {
                            // 컴파일이 성공 → 프로그램 실행
                            ProcessStartInfo psi2 = new ProcessStartInfo {
                                FileName = exeFile,
                                RedirectStandardOutput = true,
                                RedirectStandardInput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (Process exeProcess = Process.Start(psi2)) {
                                if (exeProcess != null) {
                                    // 프로그램 실행 결과를 OutputTextBox에 출력
                                    string output = exeProcess.StandardOutput.ReadToEnd();
                                    OutputTextBox.Text = output;
                                } else {
                                    OutputTextBox.Text = "프로그램 실행 실패";
                                }
                            }
                        } else {
                            // 컴파일 에러 발생 시 OutputTextBox에 에러 메시지 출력
                            OutputTextBox.Text = "컴파일 에러";
                        }
                        // 에러 메시지를 ErrorTextBox에 추가하여 출력
                        //ErrorTextBox.Text = error.ToString();
                        OutputTextBox.AppendText("\n" + error.ToString());
                    }
                }
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e) {
            // 파일을 열기 위한 OpenFileDialog 호출
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) {
                // 선택된 파일의 파일명 및 내용 표시
                FileName_Label.Content = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                CodeTextBox.Text = File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;

            foreach (TextChange change in e.Changes) {
                int offset = change.Offset;
                int addedLength = change.AddedLength;
                string addedText = textBox.Text.Substring(offset, addedLength);

                // 새로운 괄호가 추가될 때 이전에 있는 괄호 앞에 커서가 위치하도록 함
                if (addedText == "{" || addedText == "(" || addedText == "[" || addedText == "'" || addedText == "\"") {
                    int caretIndex = textBox.CaretIndex;
                    string closingBracket = addedText == "{" ? "}" : (addedText == "(" ? ")" : (addedText == "[" ? "]" : (addedText == "'" ? "'" : "\"")));
                    textBox.Text = textBox.Text.Insert(offset + addedLength, closingBracket);
                    textBox.CaretIndex = caretIndex;
                }
            }
        }

        private void ToMiniButton_Click(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void SizeButton_Click(object sender, RoutedEventArgs e) {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
                SizeButton.Content = "2";
            } else if (this.WindowState == WindowState.Normal) {
                this.WindowState = WindowState.Maximized;
                SizeButton.Content = "1";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            string sourceCode = CodeTextBox.Text;

            // 파일 이름.c 경로
            string cFile = testPath + "/" + FileName_Label.Content + ".c";
            Console.WriteLine("파일 이름: " + FileName_Label.Content);

            // .c 파일에 소스 코드 작성
            File.WriteAllText(cFile, sourceCode);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}