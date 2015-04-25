namespace Oxide.Plugins
{
    [Info("CsChatBack", "Bas", "0.1.0")]
    class CsChatBack : RustPlugin
    {
        [ChatCommand("hellocs")]
        void chat_hello(BasePlayer player, string cmd, string[] args)
        {
            player.ChatMessage("Hello back from Cs");
            //just some additional information you can use when handling the chat command
            System.Console.WriteLine("player.displayName="+player.displayName);
            System.Console.WriteLine("player.userID="+player.userID);
            System.Console.WriteLine("cmd="+cmd);
            if (args.Length==0){
                System.Console.WriteLine("Arguments: None");
            }else{
                string msg="Arguments:";
                foreach (string arg in args){
                    msg=msg+" "+arg;
                }
                System.Console.WriteLine(msg);
            }
            System.Console.WriteLine("");//flush console to prevent last line not visible
        }            
    }
}