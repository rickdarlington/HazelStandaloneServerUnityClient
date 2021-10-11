namespace UnityClient
{
    public enum MessageTags
    {
        ServerInit,     // 0
        LogIn,          // 1
        LoginSuccess,   // 2
        LoginFailed,    // 3
        ServerMessage,  // 4 
        GameData,       // 5
        ConsoleMessage, // 6
        PlayerChat
    }
}