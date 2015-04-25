PLUGIN.Title = "LuaAdminBroadcast"
PLUGIN.Author = "Bas"
PLUGIN.Version = V(0, 1, 0)
function PLUGIN:Init()
    command.AddChatCommand("broadcastlua", self.Object, "chat_broadcast")
end
function PLUGIN:chat_broadcast(player, cmd, args)
    if player.net.connection.authLevel > 0 then
        msg="Broadcast Message"
        if args.Length > 0 then
            for i=0, args.Length-1, 1 do
                msg=msg.." "..args[i]
            end
        end
        rust.BroadcastChat("LuaAdminBroadcast",msg)      
    end    
end

