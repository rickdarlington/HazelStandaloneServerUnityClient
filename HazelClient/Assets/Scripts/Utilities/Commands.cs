namespace UnityClient.Utilities
{
    public class Commands
    {
        public class SendRawToServer : ConsoleCommand
        {
            public override bool Process(string args)
            {
                HazelNetworkManager.instance.SendConsoleToServer(args);
                return true;
            }
        }
    }
}