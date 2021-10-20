namespace UnityClient
{
    public enum MessageTags
    {
        None,
        ServerInit,     // 1
        LogIn,          // 2
        LoginSuccess,   // 3
        LoginFailed,    // 4
        ServerMessage,  // 5 
        GameData,       // 6
        ConsoleMessage, // 7
        PlayerInput,    // 8
        PlayerChat
    }
}