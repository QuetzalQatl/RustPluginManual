var JsChatBack = {
    Title : "JsChatBack",
    Author : "Bas",
    Version : V(0, 1, 0),
    Init: function() {
        command.AddChatCommand("hellojs", this.Plugin, "chat_hello");
    },
    chat_hello: function(player, cmd, args){
        rust.SendChatMessage(player, "Hello back from Js", null, "0");
        /*just some additional information you can use when handling the chat command*/
        print ("player.displayName="+player.displayName);
        print ("player.userID="+player.userID);
        print ("cmd="+cmd);
        if (args.length==0) {
            print ("Arguments: None");
        }else{
            msg="Arguments:";
            for (var i = 0; i < args.length; i++) {
                msg=msg+" "+args[i];
            }
            print (msg);            
        }
    }
}