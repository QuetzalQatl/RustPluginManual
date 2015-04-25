PLUGIN.Title = "LuaHelloChat"
PLUGIN.Author = "Bas"
PLUGIN.Version = V(0, 1, 0)
function PLUGIN:Init()
    command.AddChatCommand("broadcastlua", self.Object, "chat_broadcast")
end
function PLUGIN:chat_broadcast(player, cmd, args)
    rust.SendChatMessage(player, "Hello back from Lua", null, "0")
    --just some additional information you can use when handling the chat command
    print('player.displayName='..player.displayName)
    rust.UserIDFromPlayer(player)
    print('player.userID='..rust.UserIDFromPlayer(player))--lua does not know uint player.userID, it turns up as a float
    print('cmd='..cmd)
    msg='Arguments:'
    if args.Length == 0 then
        msg=msg..' None'
    else
        for i=0, args.Length-1, 1 do
	    msg=msg.." "..args[i]
        end
    end
    print (msg)
end



