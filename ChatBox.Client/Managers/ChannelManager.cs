using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChatBox.Client.ViewModels;

namespace ChatBox.Client.Managers
{
    public class ChannelManager
    {
        private readonly ListBox _lstChatMessages;
        private readonly ScrollViewer _scrollImageGallery;
        private readonly ScrollViewer _scrollFileGallery;
        private readonly Border _borderInputArea;
        private readonly TextBlock _lblInputPlaceholder;
        private readonly TextBlock _txtChanChat;
        private readonly Border _borderChanChat;
        private readonly TextBlock _txtChanImages;
        private readonly Border _borderChanImages;
        private readonly TextBlock _txtChanFiles;
        private readonly Border _borderChanFiles;

        private readonly GalleryManager _galleryManager;
        private readonly LightboxManager _lightboxManager;

        public string CurrentChannel { get; private set; } = "chat";
        public List<ChatMessage> AllMessages { get; } = new();

        public ChannelManager(
            ListBox lstChatMessages,
            ScrollViewer scrollImageGallery,
            ScrollViewer scrollFileGallery,
            Border borderInputArea,
            TextBlock lblInputPlaceholder,
            TextBlock txtChanChat,
            Border borderChanChat,
            TextBlock txtChanImages,
            Border borderChanImages,
            TextBlock txtChanFiles,
            Border borderChanFiles,
            GalleryManager galleryManager,
            LightboxManager lightboxManager)
        {
            _lstChatMessages = lstChatMessages;
            _scrollImageGallery = scrollImageGallery;
            _scrollFileGallery = scrollFileGallery;
            _borderInputArea = borderInputArea;
            _lblInputPlaceholder = lblInputPlaceholder;
            _txtChanChat = txtChanChat;
            _borderChanChat = borderChanChat;
            _txtChanImages = txtChanImages;
            _borderChanImages = borderChanImages;
            _txtChanFiles = txtChanFiles;
            _borderChanFiles = borderChanFiles;
            _galleryManager = galleryManager;
            _lightboxManager = lightboxManager;
        }

        public void SelectChannel(string channelName, object txtInput)
        {
            CurrentChannel = channelName;

            var activeBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3E5E8"));
            var inactiveBg = Brushes.Transparent;
            var activeText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#060607"));
            var inactiveText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F5660"));

            _borderChanChat.Background = channelName == "chat" ? activeBg : inactiveBg;
            _txtChanChat.Foreground = channelName == "chat" ? activeText : inactiveText;
            _txtChanChat.FontWeight = channelName == "chat" ? FontWeights.Bold : FontWeights.Normal;

            _borderChanImages.Background = channelName == "images" ? activeBg : inactiveBg;
            _txtChanImages.Foreground = channelName == "images" ? activeText : inactiveText;
            _txtChanImages.FontWeight = channelName == "images" ? FontWeights.Bold : FontWeights.Normal;

            _borderChanFiles.Background = channelName == "files" ? activeBg : inactiveBg;
            _txtChanFiles.Foreground = channelName == "files" ? activeText : inactiveText;
            _txtChanFiles.FontWeight = channelName == "files" ? FontWeights.Bold : FontWeights.Normal;

            foreach (var msg in AllMessages)
            {
                msg.IsInImageChannel = channelName == "images";
            }

            switch (channelName)
            {
                case "chat":
                    _scrollImageGallery.Visibility = Visibility.Collapsed;
                    _scrollFileGallery.Visibility = Visibility.Collapsed;
                    _lstChatMessages.Visibility = Visibility.Visible;
                    _borderInputArea.IsEnabled = true;
                    _borderInputArea.Opacity = 1.0;
                    _lblInputPlaceholder.Text = "Message #lan-global-chat";
                    _lblInputPlaceholder.Visibility = Visibility.Visible;
                    RefreshMessageList();
                    break;

                case "images":
                    _scrollImageGallery.Visibility = Visibility.Visible;
                    _scrollFileGallery.Visibility = Visibility.Collapsed;
                    _lstChatMessages.Visibility = Visibility.Collapsed;
                    _borderInputArea.IsEnabled = false;
                    _borderInputArea.Opacity = 0.55;
                    _lblInputPlaceholder.Text = "Only images can be viewed in this channel";
                    _lblInputPlaceholder.Visibility = Visibility.Visible;
                    _galleryManager.RefreshImageGallery(AllMessages);
                    break;

                case "files":
                    _scrollImageGallery.Visibility = Visibility.Collapsed;
                    _scrollFileGallery.Visibility = Visibility.Visible;
                    _lstChatMessages.Visibility = Visibility.Collapsed;
                    _borderInputArea.IsEnabled = false;
                    _borderInputArea.Opacity = 0.55;
                    _lblInputPlaceholder.Text = "Only files can be viewed in this channel";
                    _lblInputPlaceholder.Visibility = Visibility.Visible;
                    _galleryManager.RefreshFileGallery(AllMessages);
                    break;
            }
        }

        public void RefreshMessageList()
        {
            _lstChatMessages.Items.Clear();
            foreach (var msg in AllMessages)
            {
                if (IsMessageInCurrentChannel(msg))
                {
                    _lstChatMessages.Items.Add(msg);
                }
            }
            if (_lstChatMessages.Items.Count > 0)
            {
                _lstChatMessages.ScrollIntoView(_lstChatMessages.Items[_lstChatMessages.Items.Count - 1]);
            }
        }

        public bool IsMessageInCurrentChannel(ChatMessage msg)
        {
            if (CurrentChannel == "chat") return true;
            if (CurrentChannel == "images") return msg.IsFile && msg.IsImage;
            if (CurrentChannel == "files") return msg.IsFile && !msg.IsImage;
            return false;
        }
    }
}