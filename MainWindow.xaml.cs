using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IDE
{
    public partial class MainWindow : Window
    {
        private CompletionWindow completionWindow;
        private bool isIDEInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }


        private void InitIDE()
        {
            if (isIDEInitialized) return;

            OpenGrid.Visibility = Visibility.Collapsed;
            IDEGrid.Visibility = Visibility.Visible;
            isIDEInitialized = true;
        }

        private void Menu_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "C# Dosyaları (*.cs)|*.cs|Tüm Dosyalar (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InitIDE();
                LoadFile(openFileDialog.FileName);
            }
        }

        private void Menu_NewFile_Click(object sender, RoutedEventArgs e)
        {
            InitIDE();

            string template = "using System;\n\nclass Program\n{\n    static void Main(string[] args)\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}";

            string tempPath = Path.Combine(Path.GetTempPath(), $"NewFile_{DateTime.Now.Ticks}.cs");
            File.WriteAllText(tempPath, template, Encoding.UTF8);
            LoadFile(tempPath);
        }

        private void LoadFile(string path)
        {
            foreach (TabItem tab in EditorTabControl.Items)
            {
                if (tab.Tag?.ToString() == path)
                {
                    EditorTabControl.SelectedItem = tab;
                    return;
                }
            }

            var editor = new TextEditor
            {
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#"),
                FontFamily = new FontFamily("JetBrains Mono"),
                FontSize = 14,
                ShowLineNumbers = true,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
                Foreground = Brushes.White,
                LineNumbersForeground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Thickness(5),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Tag = path
            };
            AttachEditorEvents(editor);
            editor.Text = File.ReadAllText(path);

            var tabItem = new TabItem
            {
                Header = Path.GetFileName(path),
                Content = editor,
                Tag = path // Tooltip için dosya yolu
            };

            EditorTabControl.Items.Add(tabItem);
            EditorTabControl.SelectedItem = tabItem;

            try
            {
                string directory = Path.GetDirectoryName(path);
                var fileList = Directory.GetFiles(directory);
                DirectoryExplorerPanel.ItemsSource = fileList;
            }
            catch { }
        }

        private void DirectoryExplorerPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DirectoryExplorerPanel.SelectedItem is string filePath && File.Exists(filePath))
                LoadFile(filePath);
        }

        private void SaveCurrentFile()
        {
            if (EditorTabControl.SelectedItem is TabItem tab && tab.Content is TextEditor editor && tab.Tag is string path)
            {
                File.WriteAllText(path, editor.Text, Encoding.UTF8);
                MessageBox.Show("Kaydedildi: " + path);
            }
        }

        private void CompileCurrentFile()
        {
            if (EditorTabControl.SelectedItem is TabItem tab && tab.Content is TextEditor editor)
            {
                string tempFile = Path.GetTempFileName().Replace(".tmp", ".cs");
                File.WriteAllText(tempFile, editor.Text);

                var provider = new CSharpCodeProvider();
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    OutputAssembly = Path.ChangeExtension(tempFile, ".dll"),
                    IncludeDebugInformation = true
                };
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("UnityEngine.dll");
                parameters.ReferencedAssemblies.Add("UnityEditor.dll");

                var results = provider.CompileAssemblyFromFile(parameters, tempFile);

                if (results.Errors.HasErrors)
                {
                    var sb = new StringBuilder("Derleme Hataları:\n\n");
                    foreach (CompilerError error in results.Errors)
                        sb.AppendLine($"Satır {error.Line}: {error.ErrorText}");
                    MessageBox.Show(sb.ToString());
                }
                else
                {
                    string pluginsPath = Path.Combine("UnityProject/Assets/Plugins");
                    Directory.CreateDirectory(pluginsPath);
                    string destPath = Path.Combine(pluginsPath, Path.GetFileName(results.PathToAssembly));
                    File.Copy(results.PathToAssembly, destPath, true);
                    MessageBox.Show("Başarıyla derlendi ve kopyalandı: " + destPath);
                }
            }
        }

        private void AttachEditorEvents(TextEditor editor)
        {
            editor.TextArea.TextEntered += (sender, e) =>
            {
                if (!char.IsLetter(e.Text[0])) return;
                string currentWord = GetCurrentWord(editor);
                if (string.IsNullOrWhiteSpace(currentWord)) return;

                completionWindow = new CompletionWindow(editor.TextArea);
                completionWindow.CompletionList.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                completionWindow.CompletionList.Foreground = Brushes.White;
                completionWindow.CompletionList.FontFamily = new FontFamily("JetBrains Mono");
                completionWindow.CompletionList.FontSize = 14;
                completionWindow.CompletionList.MaxHeight = 200;

                var data = completionWindow.CompletionList.CompletionData;
                string[] keywords = new string[] { "public", "private", "class", "void", "int", "float", "string", "static", "using", "namespace" };
                foreach (var k in keywords)
                    if (k.StartsWith(currentWord))
                        data.Add(new CompletionData(k));

                if (data.Count > 0) completionWindow.Show();
            };
        }

        private string GetCurrentWord(TextEditor editor)
        {
            int caret = editor.TextArea.Caret.Offset;
            var document = editor.Document;
            int startOffset = caret;
            while (startOffset > 0 && char.IsLetter(document.GetCharAt(startOffset - 1)))
                startOffset--;
            return document.GetText(startOffset, caret - startOffset);
        }

        private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrentFile();
        private void Compile_Click(object sender, RoutedEventArgs e) => CompileCurrentFile();
    }
}