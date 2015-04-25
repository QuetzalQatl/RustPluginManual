import Rust
import server
from UnityEngine import Time

# GLOBAL VARIABLES
DEV = False
LATEST_CFG = 2.0
LINE = '-' * 50

class radline:

    # ==========================================================================
    # <>> PLUGIN
    # ==========================================================================
    def __init__(self):

        # PLUGIN INFO
        self.Title = 'Rad Line'
        self.Author = 'SkinN'
        self.Description = 'Turns off radiation for a while from time to time'
        self.Version = V(1, 1, 0)
        self.HasConfig = True
        self.ResourceId = 914

    # ==========================================================================
    # <>> CONFIGURATION
    # ==========================================================================
    def LoadDefaultConfig(self):

        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': self.Title.upper(),
                'OFF TIME': 10,
                'INTERVAL': 30
            },
            'MESSAGES': {
                'RAD OFF': 'RADIATION LEVELS ARE DOWN FOR {offtime} MINUTES!',
                'RAD ON': 'RADIATION LEVELS ARE BACK UP! Will be down again in {interval} minutes.',
                'STATE ON': 'Radiation levels will be down in {remaining} minutes.',
                'STATE OFF': 'Radiation levels are now down!',
                'RAD COMMAND DESC': '/rad - Shows the current state of radiation, whether is on or off'
            },
            'COLORS': {
                'PREFIX': 'red',
                'MESSAGES': 'lime'
            }
        }

        self.console('Loading default configuration file', True)

    # --------------------------------------------------------------------------
    def UpdateConfig(self):

        # IS OLDER CONFIG TWO VERSIONS OLDER?
        if self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2 or DEV:

            self.console('Current configuration file is two or more versions older than the latest (Current: v%s / Latest: v%s)' % (self.Config['CONFIG_VERSION'], LATEST_CFG), True)

            # RESET CONFIGURATION
            self.Config.clear()

            # RESET CONFIGURATION
            self.LoadDefaultConfig()

        else:

            # NEW VERSION VALUE
            self.Config['CONFIG_VERSION'] = LATEST_CFG

            # NEW CHANGES

        # SAVE CHANGES
        self.SaveConfig()

    # ==========================================================================
    # <>> PLUGIN SPECIFIC
    # ==========================================================================
    def Init(self):

        self.console('Loading Plugin')
        self.console(LINE)

        # UPDATE CONFIG FILE
        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        # PLUGIN SPECIFIC
        global PLUGIN, MSG, COLOR
        MSG = self.Config['MESSAGES']
        COLOR = self.Config['COLORS']
        PLUGIN = self.Config['SETTINGS']

        self.prefix = '<color=%s>%s</color>' % (self.Config['COLORS']['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.title = '<color=red>%s</color>' % self.Title.upper()
        self.intstamp = 0
        self.offtime = PLUGIN['OFF TIME'] * 60 if PLUGIN['OFF TIME'] else 600
        self.int = PLUGIN['INTERVAL'] * 60 if PLUGIN['INTERVAL'] else 1800

        # START EVENT
        self.console('Starting event loop (Interval: %d.%d minute/s)' % divmod(PLUGIN['INTERVAL'] * 60, 60))
        self.loop(True)

        # COMMANDS
        self.console('Creating /rad command')
        command.AddChatCommand('rad', self.Plugin, 'rad_CMD')
        command.AddChatCommand(self.Title.lower(), self.Plugin, 'plugin_CMD')

        self.console(LINE)

    # ==========================================================================
    # <>> MESSAGE FUNTIONS
    # ==========================================================================
    def console(self, text, force=False):

        print('[%s v%s] :: %s' % (self.Title, str(self.Version), text))

    # --------------------------------------------------------------------------
    def say(self, text, color='white', userid=0):

        if self.prefix:

            rust.BroadcastChat('%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text), None, str(userid))

        else:

            rust.BroadcastChat('<color=%s>%s</color>' % (color, text), None, str(userid))

    # --------------------------------------------------------------------------
    def tell(self, player, text, color='white', userid=0, force=True):

        if self.prefix and force:

            rust.SendChatMessage(player, '%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text), None, str(userid))

        else:

            rust.SendChatMessage(player, '<color=%s>%s</color>' % (color, text), None, str(userid))

    # ==========================================================================
    # <>> MAIN FUNTIONS
    # ==========================================================================
    def loop(self, force=False):

        if not server.radiation or force:

            server.radiation = True

            self.intstamp = Time.realtimeSinceStartup

            timer.Once(self.int, self.loop, self.Plugin)

            self.console('Radiation is now on')

            if not force:

                self.say(MSG['RAD ON'].format(interval=PLUGIN['INTERVAL']), COLOR['MESSAGES'])

        else:

            server.radiation = False

            timer.Once(self.offtime, self.loop, self.Plugin)

            self.say(MSG['RAD OFF'].format(offtime=PLUGIN['OFF TIME']), COLOR['MESSAGES'])

            self.console('Radiation is now off')

    # --------------------------------------------------------------------------
    def rad_CMD(self, player, cmd, args):

        if server.radiation:

            secs = int((PLUGIN['INTERVAL'] * 60) - (Time.realtimeSinceStartup - self.intstamp))

            self.tell(player, MSG['STATE ON'].format(remaining='%d.%d' % divmod(secs, 60)), COLOR['MESSAGES'])

        else:

            self.tell(player, MSG['STATE OFF'], COLOR['MESSAGES'])

    # --------------------------------------------------------------------------
    def plugin_CMD(self, player, cmd, args):

        self.tell(player, LINE, force=False)
        self.tell(player, '<color=lime>%s v%s</color> by <color=lime>SkinN</color>' % (self.title, self.Version), force=False)
        self.tell(player, self.Description, 'lime', force=False)
        self.tell(player, '| RESOURSE ID: <color=lime>%s</color> | CONFIG: v<color=lime>%s</color> |' % (self.ResourceId, self.Config['CONFIG_VERSION']), force=False)
        self.tell(player, LINE, force=False)
        self.tell(player, '<< Click the icon to contact me.', userid='76561197999302614', force=False)

    # --------------------------------------------------------------------------
    def SendHelpText(self, player, cmd=None, args=None):

        self.tell(player, self.Config['MESSAGES']['RAD COMMAND DESC'])

# ==============================================================================0, 