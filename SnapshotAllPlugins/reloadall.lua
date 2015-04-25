PLUGIN.Title        = "Easy Reloader"
PLUGIN.Description  = "Reloads your Plugins"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,1,0)
PLUGIN.HasConfig    = false
PLUGIN.ResourceID   = 854

function PLUGIN:Init()	
 	command.AddChatCommand("reload", self.Object, "cmdReload")
end

function PLUGIN:cmdReload(player, cmd, args, PluginName)
    if args.Length == 1 then
        local PluginName = tostring(args[0])
        if not player.net.connection.authLevel == 2 then return end
        rust.RunServerCommand("oxide.reload " .. PluginName)
		rust.SendChatMessage(player, "RELOADER", "Plugin " .. PluginName .. " successfully reloaded")
    else
		rust.RunServerCommand("oxide.reload " .. "*")
		rust.SendChatMessage(player, "RELOADER", "Plugins successfully reloaded")
    end
end