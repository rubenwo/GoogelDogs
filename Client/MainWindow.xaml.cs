﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using OT.Entities;
using Protocol;
using Protocol.Messages;


namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LoginWindow _login;
        private readonly Net.Client _client;
        private readonly string _uuid = Guid.NewGuid().ToString();
        private string _previousEditorContent;
        public DispatcherTimer Timer { get; }

        public MainWindow()
        {
            _client = new Net.Client("127.0.0.1", 1337, OnLogin, AddMessageToLog, UpdateTextEditor);
            _login = new LoginWindow(_client, this);
            _login.Show();
            this.Hide();
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _previousEditorContent = TextEditor.Text;
            Timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1000)};
            Timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_previousEditorContent != TextEditor.Text)
            {
                _client.SendUpdatePatch(_previousEditorContent, TextEditor.Text);
            }

            
        }

        public void OnLogin()
        {
            Dispatcher.Invoke(Show);
            Timer.Start();
        }

        private void ChatLog_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Text Files(*.txt)|*.txt|All(*.*)|*"
            };

            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, TextEditor.Text);
            }
        }

        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            Stream myStream = null;
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            TextEditor.Text = File.ReadAllText(openFileDialog1.FileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void TextEditor_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            File.WriteAllText($"{Path.GetTempPath()}{_uuid}_GoogelDogs_local_cache.txt", TextEditor.Text);
        }

        private void ChatBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && ChatBox.Text != "")
            {
                _client.SendChatMessage(ChatBox.Text);
                ChatBox.Text = "";
            }
        }

        public void AddMessageToLog(string sender, string message)
        {
            Dispatcher.Invoke(() => { this.ChatLog.Items.Add(new {Sender = sender, Message = message}); });
        }

        public void UpdateTextEditor(PatchMessage message)
        {
            Edit edit = message.Edits.Pop();
            if (edit.ServerVersion > _client.Document.ShadowCopy.ServerVersion)
            {
                // Update Server Shadow
                List<Patch> patches = _client.DMP.patch_make(TextEditor.Text, edit.Diffs);
                string updatedText = _client.DMP.patch_apply(patches, TextEditor.Text)[0].ToString();
                Console.WriteLine(updatedText);
                Dispatcher.Invoke(() =>
                {
                    TextEditor.Text = updatedText;
                    _previousEditorContent = TextEditor.Text;
                });
                _client.Document.ShadowCopy.ServerVersion++;
            }
        }


        private void OnCreditsClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format(
                "========================={0} GoogleDogs was made by {0} Ruben Woldhuis & Bart van Es {0}=========================",
                Environment.NewLine));
        }
    }
}