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
using System.Windows.Markup;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Açık dosya var mı kontrolü (örneğin otomatik yükleme yapmıyorsan)
            OpenGrid.Visibility = Visibility.Visible;
            IDEGrid.Visibility = Visibility.Collapsed;
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

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "C# Dosyaları (*.cs)|*.cs",
                Title = "Yeni C# Dosyası Oluştur"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string template = "using System;\n\nclass Program\n{\n    static void Main(string[] args)\n    {\n        Console.WriteLine(\"Hello, World!\");\n    }\n}";

                File.WriteAllText(saveFileDialog.FileName, template, Encoding.UTF8);
                LoadFile(saveFileDialog.FileName);
            }
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
            editor.Text = File.ReadAllText(path); var tabItem = new TabItem
            {
                Header = Path.GetFileName(path),
                Content = editor,
                Tag = path // Tooltip için dosya yolu
            };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = System.IO.Path.GetFileName(path),
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = Brushes.White
            });
            var closeBtn = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => EditorTabControl.Items.Remove(tabItem);
            headerPanel.Children.Add(closeBtn);

            tabItem.Header = headerPanel;




            EditorTabControl.Items.Add(tabItem);
            EditorTabControl.SelectedItem = tabItem;

            UpdateFileTree(Path.GetDirectoryName(path));
        }

        private void UpdateFileTree(string rootPath)
        {
            if (!Directory.Exists(rootPath)) return;

            TreeViewItem CreateNode(string path)
            {
                TreeViewItem item = new TreeViewItem
                {
                    Header = new TextBlock
                    {
                        Text = System.IO.Path.GetFileName(path),
                        FontSize = 14,
                        Margin = new Thickness(4),
                        Foreground = Brushes.White
                    },
                    Tag = path
                };

                if (Directory.Exists(path))
                {
                    foreach (var dir in Directory.GetDirectories(path))
                        item.Items.Add(CreateNode(dir));
                    foreach (var file in Directory.GetFiles(path))
                        item.Items.Add(new TreeViewItem { Header = System.IO.Path.GetFileName(file), Tag = file });
                }
                return item;
            }

            FileTree.Items.Clear();
            FileTree.Items.Add(CreateNode(rootPath));
        }

        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FileTree.SelectedItem is TreeViewItem item && item.Tag is string path && File.Exists(path))
                LoadFile(path);
        }

        private void DirectoryExplorerPanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

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
            ErrorOutput.Text = "";
            ConsoleOutput.Text = "";

            if (EditorTabControl.SelectedItem is TabItem tab && tab.Content is TextEditor editor)
            {
                string tempFile = Path.GetTempFileName().Replace(".tmp", ".cs");
                File.WriteAllText(tempFile, editor.Text);

                var provider = new CSharpCodeProvider();
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = true, // DLL değil EXE üret
                    OutputAssembly = Path.ChangeExtension(tempFile, ".exe"),
                    IncludeDebugInformation = true,
                    CompilerOptions = "/t:exe"
                };

                parameters.ReferencedAssemblies.Add("System.dll");

                string unityDllPath = @"C:\Program Files\Unity\Hub\Editor\6000.0.29f1\Editor\Data\Managed\UnityEngine";
                foreach (var dll in Directory.GetFiles(unityDllPath, "*.dll"))
                {
                    parameters.ReferencedAssemblies.Add(dll);
                }

                var results = provider.CompileAssemblyFromFile(parameters, tempFile);

                if (results.Errors.HasErrors)
                {
                    var sb = new StringBuilder();
                    foreach (CompilerError error in results.Errors)
                        sb.AppendLine($"Satır {error.Line}: {error.ErrorText}");
                    ErrorOutput.Text = sb.ToString();
                }
                else
                {
                    ErrorOutput.Text = "Derleme başarılı.";

                    try
                    {
                        var assembly = Assembly.LoadFile(results.PathToAssembly);
                        var entryPoint = assembly.EntryPoint;

                        if (entryPoint != null)
                        {
                            using (var writer = new StringWriter())
                            {
                                Console.SetOut(writer);

                                var parameters2 = entryPoint.GetParameters().Length == 0 ? null : new object[] { new string[0] };
                                entryPoint.Invoke(null, parameters2);

                                ConsoleOutput.Text = writer.ToString();
                            }
                        }
                        else
                        {
                            ConsoleOutput.Text = "Main metodu (entryPoint) bulunamadı.";
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleOutput.Text = $"Çalıştırma hatası: {ex.Message}";
                    }
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
                string[] keywords = new string[]
{
    "public", "private", "protected", "class", "interface", "enum", "struct",
    "void", "int", "float", "double", "decimal", "string", "bool",
    "static", "const", "readonly", "new", "override", "virtual",
    "namespace", "using", "return", "if", "else", "switch", "case",
    "break", "continue", "for", "foreach", "while", "do", "try", "catch", "finally",
    "null", "true", "false", "this", "base", "get", "set"
};
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