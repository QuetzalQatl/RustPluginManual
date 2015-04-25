class PyAdminBroadcast:
    def __init__(self):
        self.Title = 'PyAdminBroadcast'
        self.Version = V(0, 0, 1)
        self.Author = 'Bas'
    def Init(self):
        command.AddChatCommand('broadcastpy', self.Plugin, 'chat_broadcast')
    def chat_broadcast(self, player, cmd, args):
        msg="Broadcast Message "
        if args:
            msg=msg+' '.join(args)
        rust.BroadcastChat("PyAdminBroadcast",msg)      
