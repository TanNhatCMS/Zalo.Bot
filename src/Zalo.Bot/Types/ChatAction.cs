namespace Zalo.Bot.Types;

/// <summary>Constants for sendChatAction (docs apis/sendChatAction).</summary>
public static class ChatAction
{
    /// <summary>Bot đang soạn tin nhắn văn bản ("typing")</summary>
    public const string Typing = "typing";

    /// <summary>Bot đang tải ảnh lên ("upload_photo" — docs ghi "sắp ra mắt", nền tảng có thể chưa nhận)</summary>
    public const string UploadPhoto = "upload_photo";
}
