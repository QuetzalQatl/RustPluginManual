class PyHelloChat:
    def __init__(self):
        self.Title = 'PyHelloChat'
        self.Version = V(0, 0, 1)
        self.Author = 'Bas'
    def Init(self):
        command.AddChatCommand('hellopy', self.Plugin, 'chat_hello')
    def chat_hello(self, player, cmd, args):
        rust.SendChatMessage(player, "Hello back from Py", None, "0")
        #just some additional information you can use when handling the chat command
        print 'player.displayName='+player.displayName
        print 'player.userID='+str(player.userID)
        print 'cmd='+cmd
        if args:
            print('Arguments: '+', '.join(args))
        else:
            print('Arguments: None')
        