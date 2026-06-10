using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using ChatBox.Client.ViewModels;

namespace ChatBox.Client.Managers
{
    public class GalleryManager
    {
        private readonly ItemsControl _itemsImageGallery;
        private readonly ItemsControl _itemsFileGallery;

        public GalleryManager(ItemsControl itemsImageGallery, ItemsControl itemsFileGallery)
        {
            _itemsImageGallery = itemsImageGallery;
            _itemsFileGallery = itemsFileGallery;
        }

        public void RefreshImageGallery(System.Collections.Generic.List<ChatMessage> allMessages)
        {
            var imageMessages = allMessages.Where(m => m.IsImage).ToList();
            var groups = imageMessages.GroupBy(m => m.RawDate.ToLocalTime().Date)
                                      .OrderByDescending(g => g.Key);

            var list = new ObservableCollection<ImageGroup>();
            foreach (var g in groups)
            {
                string header = GetDateHeader(g.Key);
                var imgGroup = new ImageGroup { DateHeader = header };
                foreach (var m in g.OrderBy(msg => msg.RawDate))
                {
                    imgGroup.Images.Add(m);
                }
                list.Add(imgGroup);
            }

            _itemsImageGallery.ItemsSource = list;
        }

        public void RefreshFileGallery(System.Collections.Generic.List<ChatMessage> allMessages)
        {
            var fileMessages = allMessages.Where(m => m.IsFile && !m.IsImage).ToList();
            var groups = fileMessages.GroupBy(m => m.RawDate.ToLocalTime().Date)
                                     .OrderByDescending(g => g.Key);

            var list = new ObservableCollection<FileGroup>();
            foreach (var g in groups)
            {
                string header = GetDateHeader(g.Key);
                var fg = new FileGroup { DateHeader = header };
                foreach (var m in g.OrderBy(msg => msg.RawDate))
                {
                    fg.Files.Add(m);
                }
                list.Add(fg);
            }

            _itemsFileGallery.ItemsSource = list;
        }

        private static string GetDateHeader(DateTime date)
        {
            if (date == DateTime.Today) return "Today";
            if (date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return date.ToString("MMMM dd, yyyy");
        }

        public class ImageGroup
        {
            public string DateHeader { get; set; } = "";
            public ObservableCollection<ChatMessage> Images { get; set; } = new();
        }

        public class FileGroup
        {
            public string DateHeader { get; set; } = "";
            public ObservableCollection<ChatMessage> Files { get; set; } = new();
        }
    }
}