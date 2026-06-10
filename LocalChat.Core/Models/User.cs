using System;
using System.ComponentModel.DataAnnotations;

namespace LocalChat.Core.Models
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        // Lưu ảnh dưới dạng chuỗi Base64 để nhúng thẳng vào giao diện mà không cần tải file phụ
        public string? AvatarBase64 { get; set; } 

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
