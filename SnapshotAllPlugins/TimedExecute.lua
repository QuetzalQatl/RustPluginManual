PLUGIN.Title = "TimedExecute"
PLUGIN.Version = V(0, 1, 0)
PLUGIN.Description = "Executes a command every (x) seconds."
PLUGIN.Author = "Merka"
PLUGIN.HasConfig = true

function PLUGIN:LoadDefaultConfig()
    self.Config.ShowTimedCommands = "true"
    self.Config.TimedCommands = { 
{"server.save",300},
{"say 'hello world'",600},
}
end

function PLUGIN:OnServerInitialized()
    self:TimedCommands()
end

function PLUGIN:Init()
     self.timers = {}
end

function PLUGIN:Unload()
self:ResetTimers()
end

function PLUGIN:ResetTimers()
for k,v in pairs(self.timers) do
self.timers[k]:Destroy()
end
end



function PLUGIN:TimedCommands()
    self:ResetTimers()
for k,v in pairs(self.Config.TimedCommands) do 
self.timers[k] = timer.Repeat(v[2], 0, function()
        rust.RunServerCommand(v[1])
    end, self.Plugin)
end
end