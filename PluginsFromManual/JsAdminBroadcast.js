var JsAdminBroadcast = {
    Title : "JsAdminBroadcast",
    Version : V(0, 1, 0),
    Author : "Bas",
    Init: function() {
        command.AddChatCommand("broadcastjs", this.Plugin, "chat_broadcast");
    },
    chat_broadcast: function(player, cmd, args)
    {
        if (player.net.connection.authLevel > 0)
        {
            msg="Broadcast Message";
            if ( args.length > 0)
            {
                for (var i = 0; i < args.length; i++)
                {
                    msg=msg+" "+args[i];
                }
            }
            rust.BroadcastChat("JsAdminBroadcast",msg);
        }
    }
}
