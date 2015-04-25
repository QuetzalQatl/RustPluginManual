namespace Oxide.Plugins
{
    [Info("CsAdminBroadcast", "Bas", "0.1.0")]
    class CsAdminBroadcast : RustPlugin
    {
        [ChatCommand("broadcastcs")]
        void chat_broadcast(BasePlayer player, string cmd, string[] args) {
            if (player.net.connection.authLevel > 0) {
                string name = "CsAdminBroadcast";
                string msg="Broadcast Message";
                if (args.Length>0) {
                    foreach (string arg in args) {
                        msg=msg+" "+arg;
                    }                    
                }
                string userID = "0";
                PrintToChat(name+" "+msg, userID);
            }
        }            
    }
}




