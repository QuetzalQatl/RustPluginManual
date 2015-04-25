PLUGIN.Name = "Auto Broadcast"
PLUGIN.Title = "Auto Broadcast"
PLUGIN.Version = V(0, 1, 1)
PLUGIN.Description = "Sending global broadcasts on a timer"
PLUGIN.Author = "Taffy"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 684

--** Load default configuration
function PLUGIN:LoadDefaultConfig()
	self.Config.ChatName 			= self.Config.ChatName or "Info"
	self.Config.BroadCastInterval	= self.Config.BroadCastInterval or 600
	-- Messages to send - Add or remove as required
    self.Config.Messages = {
        Message1			= "Please do not grief other players",
        Message2		    = "New plugins are added all the time. type /help for info",
        Message3			= "This is an example global broadcast"
    }
end

--**Initialisation routine
function PLUGIN:Init()
	--define some variables for counters
	x = 0
	y = 1
	-- work out how many messages have been created
	for k , v in pairs(self.Config.Messages) do
			x = x + 1
	end
	local BroadCastTimer = tonumber(self.Config.BroadCastInterval)
	local BroadCastEnabled = self.Config.TimedBroadCastEnabled
	--initiating the timer
	self.BCTimer = {}
	self.BCTimer = timer.Repeat (BroadCastTimer , 0 , function() self:BroadCastMessageNow( ) end )
end

--** Process sending of broadcast
function PLUGIN:BroadCastMessageNow( )
	--working out which message to send
	local MessageSent = 0
	if y + 1 > x then
		MessageString="Message" ..(y)
		y = 1
	else
		MessageString="Message" ..(y)
		y = y + 1
	end
	print (MessageString)
	global.ConsoleSystem.Broadcast("chat.add \"" .. self.Config.ChatName .. "\" \"" .. self.Config.Messages[MessageString] .. "\"")
end

--when unloading the plugin the timer will be destroyed
function PLUGIN:Unload()
	if self.BCTimer then self.BCTimer:Destroy() end
end