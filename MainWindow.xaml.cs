using ICSharpCode.AvalonEdit;
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
        private List<string> recentFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            AddToolbarButtons();
        }

        private void AddToolbarButtons()
        {
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 10, 0)
            };

            var saveBtn = new Button
            {
                Content = "💾 Kaydet",
                Background = Brushes.DarkSlateGray,
                Foreground = Brushes.White,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(5)
            };
            saveBtn.Click += (s, e) => SaveCurrentFile();

            var compileBtn = new Button
            {
                Content = "⚙️ Derle",
                Background = Brushes.DarkOliveGreen,
                Foreground = Brushes.White,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(5)
            };
            compileBtn.Click += (s, e) => CompileCurrentFile();

            toolbar.Children.Add(saveBtn);
            toolbar.Children.Add(compileBtn);

            Grid.SetRow(toolbar, 1); // Yeni eklenen buton bar satırı
            MainGrid.Children.Add(toolbar);
        }


        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "C# Dosyaları (*.cs)|*.cs|Tüm Dosyalar (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void LoadFile(string path)
        {
            if (!File.Exists(path)) return;

            foreach (TabItem tab in EditorTabControl.Items)
            {
                if (tab.Tag != null && tab.Tag.ToString() == path)
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
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Tag = path
            };

            editor.Text = File.ReadAllText(path);

            var tabItem = new TabItem
            {
                Header = Path.GetFileName(path),
                Content = editor,
                Tag = path
            };

            EditorTabControl.Items.Add(tabItem);
            EditorTabControl.SelectedItem = tabItem;

            if (!recentFiles.Contains(path))
            {
                recentFiles.Add(path);
                RecentFilesList.Items.Add(path);
            }

            RecentFilesList.SelectedItem = path;
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

                CompilerResults results = provider.CompileAssemblyFromFile(parameters, tempFile);

                if (results.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder("Derleme Hataları:\n\n");
                    foreach (CompilerError error in results.Errors)
                        sb.AppendLine($"Satır {error.Line}: {error.ErrorText}");

                    MessageBox.Show(sb.ToString(), "Derleme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    string pluginsPath = Path.Combine(Directory.GetCurrentDirectory(), "UnityProject/Assets/Plugins");
                    Directory.CreateDirectory(pluginsPath);
                    string destPath = Path.Combine(pluginsPath, Path.GetFileName(results.PathToAssembly));
                    File.Copy(results.PathToAssembly, destPath, true);
                    MessageBox.Show("Derleme başarılı! DLL Unity'ye kopyalandı: " + destPath);
                }
            }
        }

        private void RecentFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentFilesList.SelectedItem != null)
            {
                string selectedPath = RecentFilesList.SelectedItem.ToString();
                LoadFile(selectedPath);
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e) =>
            this.WindowState = WindowState.Minimized;

        private void WindowDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}