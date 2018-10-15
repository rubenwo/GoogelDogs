﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Client.Entities;
using DiffMatchPatch;
using Protocol;
using Protocol.Messages;

namespace Client.Net
{
    public class Client
    {
        public bool Running { get; set; } = true;
        public Document Document { get; }
        private readonly Action _loginCallback;
        private readonly Action<string, string> _messageLogCallback;
        public string Username { get; set; }
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly diff_match_patch _dmp;

        public Client(string hostname, int port, Action loginCallback,
            Action<string, string> messageLogCallback)
        {
            _loginCallback = loginCallback;
            _messageLogCallback = messageLogCallback;
            Document = new Document();
            _tcpClient = new TcpClient(hostname, port);
            Console.WriteLine(_tcpClient.Connected);
            _stream = _tcpClient.GetStream();
            _dmp = new diff_match_patch();
            StartBackgroundListener();
        }

        #region Commands

        public void Login(string username, string password, int session)
        {
            Console.WriteLine("Logging in!");
            Username = username;
            SendMessage(new LoginMessage(username, password, session));
        }

        public void SendUpdatePatch(string previousText, string currentText)
        {
            Console.WriteLine("Generating diffs");

            List<Diff> diffs = _dmp.diff_main(previousText, currentText);
            SendMessage(new PatchMessage(Username, diffs));
            Console.WriteLine("Done sending patch");
        }

        public void SendChatMessage(string message)
        {
            SendMessage(new ChatMessage(Username, message));
        }

        #endregion

        private async void SendMessage(IMessage message)
        {
            await MessagingUtil.SendMessage(_stream, message);
        }

        private void StartBackgroundListener()
        {
            Console.WriteLine("Connected!");
            Task.Factory.StartNew(async () =>
            {
                while (Running)
                {
                    dynamic msg = await MessagingUtil.ReceiveMessage(_stream);
                    Console.WriteLine($"In Client BackgroundListener: {msg}");
                    IMessage message = JsonDecoder.Decode(msg);
                    Console.WriteLine($"In Client BackgroundListener: {message.Type}");
                    switch (message.Type)
                    {
                        case MessageType.OK_MESSAGE:
                            HandleOkMessage((OkMessage) message);
                            break;
                        case MessageType.OK_LOGIN_MESSAGE:
                            HandleOkLoginMessage((OkLoginMessage) message);
                            break;
                        case MessageType.ERROR_MESSAGE:
                            HandleErrorMessage((ErrorMessage) message);
                            break;
                        case MessageType.CHAT_MESSAGE:
                            HandleChatMessage((ChatMessage) message);
                            break;
                        case MessageType.PATCH_MESSAGE:
                            HandlePatchMessage((PatchMessage) message);
                            break;
                    }
                }
            });
        }

        #region Handlers

        private void HandleOkMessage(OkMessage message)
        {
        }

        private void HandleOkLoginMessage(OkLoginMessage message)
        {
            Console.WriteLine("Login approved!");
            _loginCallback();
        }

        private void HandleErrorMessage(ErrorMessage message)
        {
        }

        private void HandleChatMessage(ChatMessage message)
        {
            _messageLogCallback(message.Sender, message.Message);
        }

        private void HandlePatchMessage(PatchMessage message)
        {
        }

        #endregion
    }
}