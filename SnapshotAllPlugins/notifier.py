import re
import Rust
import BasePlayer
import server
import UnityEngine.Random as random
from System import Action, Int32, String

# GLOBAL VARIABLES
DEV = False
LATEST_CFG = 3.7
LINE = '-' * 50

class notifier:

    # ==========================================================================
    # <>> PLUGIN
    # ==========================================================================
    def __init__(self):

        # PLUGIN INFO
        self.Title = 'Notifier'
        self.Version = V(2, 7, 0)
        self.Author = 'SkinN'
        self.Description = 'Broadcasts chat messages as notifications and advertising.'
        self.ResourceId = 797

    # ==========================================================================
    # <>> CONFIGURATION
    # ==========================================================================
    def LoadDefaultConfig(self):

        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': self.Title.upper(),
                'BROADCAST TO CONSOLE': True,
                'OWNER TAG': '[OWNER]',
                'MODERATOR TAG': '[MOD]',
                'ADMINS TALK PREFIX': '[ADMINS]',
                'ADMINS TALK TRIGGER': '@',
                'RULES LANGUAGE': 'AUTO',
                'HIDE ADMINS CONNECTIONS': False,
                'CHAT PLAYERS LIST': True,
                'CONSOLE PLAYERS LIST': True,
                'ADVERTS INTERVAL': 5,
                'ADMINS TALK AUTH LEVEL': 0,
                'ENABLE JOIN MESSAGE': True,
                'ENABLE LEAVE MESSAGE': True,
                'ENABLE WELCOME MESSAGE': True,
                'ENABLE ADVERTS': True,
                'ENABLE HELPTEXT': True,
                'ENABLE ADMIN TAGS': False,
                'ENABLE SEED CMD': False,
                'ENABLE PLAYERS LIST CMD': True,
                'ENABLE ADMINS LIST CMD': True,
                'ENABLE PLUGINS LIST CMD': False,
                'ENABLE RULES CMD': True,
                'ENABLE SERVER MAP CMD': True
            },
            'MESSAGES': {
                'JOIN MESSAGE': '<lime>{username}<end> joined the server, from <lime>{country}<end>.',
                'LEAVE MESSAGE': '<lime>{username}<end> left the server.',
                'SERVER SEED': 'The server seed is {seed}',
                'NO ADMINS ONLINE': 'There are no <cyan>Admins<end> currently online.',
                'ONLY PLAYER': 'You are the only survivor online.',
                'CHECK CONSOLE NOTE': 'Check the console (press F1) for more info.',
                'PLAYERS COUNT': 'There are <lime>{active}<end> survivors online.',
                'PLAYERS STATS': '<orange>TOTAL OF PLAYERS:<end> <lime>{total}<end> <yellow>|<end> <orange>SLEEPERS:<end> <lime>{sleepers}<end>',
                'NO RULES': 'No rules have been found!.',
                'NO LANG': 'Language not found in rules list.',
                'ADMINS LIST TITLE': 'ADMINS ONLINE',
                'PLUGINS LIST TITLE': 'SERVER PLUGINS',
                'PLAYERS LIST TITLE': 'PLAYERS ONLINE',
                'RULES TITLE': 'SERVER RULES',
                'SERVER MAP': 'SERVER MAP: <lime>{ip}:{port}<end>',
                'PLAYERS LIST DESC': '<white>/players -<end> Lists all the players. (Chat/Console)',
                'ADMINS LIST DESC': '<white>/admins -<end> Lists all the Admins currently online.',
                'PLUGINS LIST DESC': '<white>/plugins -<end> Lists all the server plugins.',
                'RULES DESC': '<white>/rules -<end> Lists the server rules.',
                'SEED DESC': '<white>/seed -<end> Shows current server seed. (Unless it is Random)',
                'SERVER MAP DESC': '<white>/map -<end> Shows the server map link.'
            },
            'WELCOME MESSAGE': (
                'Welcome <lime>{username}<end>, to the server!',
                'Type <red>/help<end> for all available commands.',
                'SERVER IP: <cyan>{ip}:{port}<end>'
            ),
            'ADVERTS': (
                'Want to know the available commands? Type <red>/help<end>.',
                'Respect the server <red>/rules<end>.',
                'This server is running <orange>Oxide 2<end>.',
                'Cheating is strictly prohibited.',
                'Type <red>/map<end> for the server map link.',
                'Players Online: <lime>{players} / {maxplayers}<end> Sleepers: <lime>{sleepers}<end>'
            ),
            'COLORS': {
                'PREFIX': 'red',
                'JOIN MESSAGE': '#CECECE',
                'LEAVE MESSAGE': '#CECECE',
                'WELCOME MESSAGE': '#CECECE',
                'ADVERTS': '#CECECE',
                'ADMINS TALK PREFIX': '#CECECE',
                'ADMINS TALK TEXT': 'white',
                'MODERATOR NAME': '#52A6FC',
                'OWNER NAME': '#AEFF56',
                'MODERATOR TAG': '#52A6FC',
                'OWNER TAG': '#AEFF56'
            },
            'COMMANDS': {
                'PLAYERS LIST': 'players',
                'RULES': ('rules', 'regras', 'regles'),
                'PLUGINS LIST': 'plugins',
                'SEED': 'seed',
                'ADMINS LIST': 'admins',
                'SERVER MAP': 'map'
            },
            'RULES': {
                'EN': (
                    'Cheating is strictly prohibited.',
                    'Respect all players',
                    'Avoid spam in chat.',
                    'Play fair and don\'t abuse of bugs/exploits.'
                ),
                'PT': (
                    'Usar cheats e totalmente proibido.',
                    'Respeita todos os jogadores.',
                    'Evita spam no chat.',
                    'Nao abuses de bugs ou exploits.'
                ),
                'FR': (
                    'Tricher est strictement interdit.',
                    'Respectez tous les joueurs.',
                    'Évitez le spam dans le chat.',
                    'Jouer juste et ne pas abuser des bugs / exploits.'
                ),
                'ES': (
                    'Los trucos están terminantemente prohibidos.',
                    'Respeta a todos los jugadores.',
                    'Evita el Spam en el chat.',
                    'Juega limpio y no abuses de bugs/exploits.'
                ),
                'DE': (
                    'Cheaten ist verboten!',
                    'Respektiere alle Spieler',
                    'Spam im Chat zu vermeiden.',
                    'Spiel fair und missbrauche keine Bugs oder Exploits.'
                ),
                'TR': (
                    'Hile kesinlikle yasaktır.',
                    'Tüm oyuncular Saygı.',
                    'Sohbet Spam kaçının.',
                    'Adil oynayın ve böcek / açıkları kötüye yok.'
                ),
                'IT': (
                    'Cheating è severamente proibito.',
                    'Rispettare tutti i giocatori.',
                    'Evitare lo spam in chat.',
                    'Fair Play e non abusare di bug / exploit.'
                ),
                'DK': (
                    'Snyd er strengt forbudt.',
                    'Respekt alle spillere.',
                    'Undgå spam i chatten.',
                    'Play fair og ikke misbruger af bugs / exploits.'
                ),
                'RU': (
                    'Запрещено использовать читы.',
                    'Запрещено спамить и материться.',
                    'Уважайте других игроков.',
                    'Играйте честно и не используйте баги и лазейки.'
                ),
                'NL': (
                    'Vals spelen is ten strengste verboden.',
                    'Respecteer alle spelers',
                    'Vermijd spam in de chat.',
                    'Speel eerlijk en maak geen misbruik van bugs / exploits.'
                ),
                'UA': (
                    'Обман суворо заборонено.',
                    'Поважайте всіх гравців',
                    'Щоб уникнути спаму в чаті.',
                    'Грати чесно і не зловживати помилки / подвиги.'
                )
            }
        }

        self.console('* Loading default configuration file', True)

    # --------------------------------------------------------------------------
    def UpdateConfig(self):

        if (self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2) or DEV:

            if not DEV:

                self.console('* Configuration file is too old, replacing to default file (Current: v%s / Latest: v%s)' % (self.Config['CONFIG_VERSION'], LATEST_CFG), True)

            self.Config.clear()

            self.LoadDefaultConfig()

        else:

            self.console('* Applying new changes to configuration file (Version: %s)' % LATEST_CFG, True)

            self.Config['CONFIG_VERSION'] = LATEST_CFG

            self.Config['SETTINGS']['ADVERTS INTERVAL'] = 5
            self.Config['SETTINGS']['ADMINS TALK PREFIX'] = '[ADMINS]'
            self.Config['SETTINGS']['ADMINS TALK TRIGGER'] = '@'
            self.Config['SETTINGS']['ENABLE JOIN MESSAGE'] = True
            self.Config['SETTINGS']['ENABLE LEAVE MESSAGE'] = True
            self.Config['SETTINGS']['ADMINS TALK AUTH LEVEL'] = 1
            self.Config['MESSAGES']['JOIN MESSAGE'] = '<lime>{username}<end> joined the server, from <lime>{country}<end>.'
            self.Config['MESSAGES']['LEAVE MESSAGE'] = '<lime>{username}<end> left the server.'
            self.Config['COLORS']['JOIN MESSAGE'] = '#CECECE'
            self.Config['COLORS']['LEAVE MESSAGE'] = '#CECECE'
            self.Config['COLORS']['ADMINS TALK PREFIX'] = '#CECECE'
            self.Config['COLORS']['ADMINS TALK TEXT'] = 'white'
            self.Config['COLORS']['MODERATOR NAME'] = '#52A6FC'
            self.Config['COLORS']['OWNER NAME'] = '#AEFF56'
            self.Config['COLORS']['OWNER TAG'] = '#AEFF56'
            self.Config['COLORS']['MODERATOR TAG'] = '#52A6FC'

            del self.Config['SETTINGS']['SHOW CONNECTED']
            del self.Config['SETTINGS']['SHOW DISCONNECTED']
            del self.Config['MESSAGES']['CONNECTED']
            del self.Config['MESSAGES']['DISCONNECTED']
            del self.Config['COLORS']['CONNECTED MESSAGE']
            del self.Config['COLORS']['DISCONNECTED MESSAGE']

        self.SaveConfig()

    # ==========================================================================
    # <>> PLUGIN SPECIFIC
    # ==========================================================================
    def Init(self):

        self.console('Loading Plugin')
        self.console(LINE)

        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:
            self.UpdateConfig()

        global MSG, PLUGIN, COLOR
        MSG = self.Config['MESSAGES']
        COLOR = self.Config['COLORS']
        PLUGIN = self.Config['SETTINGS']

        self.prefix = '<color=%s>%s</color>' % (COLOR['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.cache = {}
        self.connected = []
        self.lastadvert = 0

        self.countries = data.GetData('notifier_countries_db')
        self.countries.update(self.countries_dict())
        data.SaveData('notifier_countries_db')
        self.console('* Updating countries database')

        for player in self.player_list():
            self.cache_player(player.net.connection)
            self.get_country(player, False)
            self.connected.append(self.player_id(player))
        self.console('* Caching active players')

        if PLUGIN['ENABLE ADVERTS']:
            mins = PLUGIN['ADVERTS INTERVAL']
            secs = mins * 60 if mins else 60
            self.adverts_loop = timer.Repeat(secs, 0, Action(self.send_advert), self.Plugin)
            self.console('* Starting Adverts loop, set to %s minute/s' % mins)
        else:
            self.adverts_loop = None
            self.console('* Adverts are disabled')

        self.cmds = []
        self.console('* Enabling commands:')
        if PLUGIN['ENABLE RULES CMD']:
            self.console('  - /%s (Server Rules)' % ', /'.join(self.Config['COMMANDS']['RULES']))
            for cmd in self.Config['COMMANDS']['RULES']:
                command.AddChatCommand(cmd, self.Plugin, 'rules_CMD')
            self.cmds.append('RULES')

        for cmd in [x for x in self.Config['COMMANDS'].keys() if x != 'RULES']:
            if PLUGIN['ENABLE %s CMD' % cmd]:
                self.cmds.append(cmd)
                command.AddChatCommand(self.Config['COMMANDS'][cmd], self.Plugin, '%s_CMD' % cmd.replace(' ', '_').lower())

        n = '%s' % self.Title.lower()
        command.AddChatCommand(n, self.Plugin, 'plugin_CMD')

        if self.cmds:
            for cmd in [x for x in self.cmds if x != 'RULES']:
                self.console('  - /%s (%s)' % (self.Config['COMMANDS'][cmd], cmd.title()))
        else:
            self.console('  - No commands enabled')

        self.console(LINE)

    # --------------------------------------------------------------------------
    def Unload(self):

        if self.adverts_loop:
            self.adverts_loop.Destroy()

    # ==========================================================================
    # <>> MESSAGE FUNTIONS
    # ==========================================================================
    def console(self, text, force=False):

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or force:
            print('[%s v%s] :: %s' % (self.Title, str(self.Version), self._format(text, True)))

    # --------------------------------------------------------------------------
    def pconsole(self, player, text, color='white'):

        player.SendConsoleCommand(self._format('echo <color=%s>%s</color>' % (color, text)))

    # --------------------------------------------------------------------------
    def say(self, text, color='white', userid=0, force=True):

        if self.prefix and force:
            string = self._format('%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text))
            rust.BroadcastChat(string, None, str(userid))
        else:
            rust.BroadcastChat(self._format('<color=%s>%s</color>' % (color, text)), None, str(userid))
        self.console(self._format(text, True))

    # --------------------------------------------------------------------------
    def tell(self, player, text, color='white', userid=0, force=True):

        if self.prefix and force:
            rust.SendChatMessage(player, self._format('%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text)), None, str(userid))
        else:
            rust.SendChatMessage(player, self._format('<color=%s>%s</color>' % (color, text)), None, str(userid))

    # --------------------------------------------------------------------------
    def _format(self, text, con=False):

        name = r'\<(\w+)\>'
        hexcode = r'\<(#\w+)\>'
        end = '<end>'

        if con:
            for x in (end, name, hexcode):
                text = re.sub(x, '', text)
        else:
            text = text.replace(end, '</color>')
            for f in (name, hexcode):
                for c in re.findall(f, text):
                    text = text.replace('<%s>' % c, '<color=%s>' % c)
        return text

    # ==========================================================================
    # <>> PLAYER HOOKS
    # ==========================================================================
    def OnPlayerConnected(self, packet):

        self.cache_player(packet.connection)

    # --------------------------------------------------------------------------
    def OnPlayerInit(self, player):

        raw_name = player.displayName
        self.get_country(player)
        if PLUGIN['ENABLE WELCOME MESSAGE']:
            l = self.Config['WELCOME MESSAGE']
            if l:
                for line in l:
                    line = line.format(ip=str(server.ip), port=str(server.port), seed=str(server.seed) if server.seed else 'Random', username=raw_name)
                    self.tell(player, line, COLOR['WELCOME MESSAGE'])
            else:
                self.console('Welcome Message list is empty, disabling Welcome Message')
                PLUGIN['ENABLE WELCOME MESSAGE'] = False
        self.connected.append(self.player_id(player))

    # --------------------------------------------------------------------------
    def OnPlayerDisconnected(self, player):

        steamid = self.player_id(player)
        if steamid in self.cache:
            if steamid in self.connected:
                self.connected.remove(steamid)
                target = self.cache[steamid]
                if PLUGIN['ENABLE LEAVE MESSAGE']:
                    if not (PLUGIN['HIDE ADMINS CONNECTIONS'] and int(target['auth']) > 0):
                        if target['country'] in self.countries:
                            target['country'] = self.countries[target['country']]
                        self.say(MSG['LEAVE MESSAGE'].format(**target), COLOR['LEAVE MESSAGE'], steamid)
            del self.cache[steamid]

    # --------------------------------------------------------------------------
    def OnPlayerChat(self, args):

        msg = args.GetString(0, 'text')
        target = self.cache[self.player_id(args.connection.player)]
        text = None
        if target['auth'] > 0:
            steamid = target['steamid']
            x = PLUGIN['ADMINS TALK TRIGGER']
            if x and msg.startswith(x) and target['auth'] and target['auth'] >= PLUGIN['ADMINS TALK AUTH LEVEL']:
                text = '<%s>%s<end> :<%s>%s<end>' % (COLOR['ADMINS TALK PREFIX'], PLUGIN['ADMINS TALK PREFIX'], COLOR['ADMINS TALK TEXT'], msg.replace(x, ''))
                steamid = '0'
            elif PLUGIN['ENABLE ADMIN TAGS']:
                text = '%s : %s' % (target['adminname'], msg)
            else:
                if target['auth'] == 1:
                    c = COLOR['MODERATOR NAME']
                else:
                    c = COLOR['OWNER NAME']
                text = '<%s>%s<end> : %s' % (c, target['username'], msg)
            if text:
                rust.BroadcastChat(self._format(text), None, steamid)
                return ''

    # ==========================================================================
    # <>> MAIN FUNTIONS
    # ==========================================================================
    def send_advert(self):

        l = self.Config['ADVERTS']
        if l:
            index = self.lastadvert
            count = len(l)
            if count > 1:
                while index == self.lastadvert:
                    index = random.Range(0, len(l))
                self.lastadvert = index
            self.say(l[index].format(**{
                        'ip': server.ip,
                        'port': server.port,
                        'seed': server.seed if server.seed else 'Random',
                        'players': len(self.player_list()),
                        'maxplayers': server.maxplayers,
                        'sleepers': len(BasePlayer.sleepingPlayerList)
                    }), COLOR['ADVERTS'])
        else:
            self.console('The Adverts list is empty, stopping Adverts loop')
            self.adverts_loop.Destroy()

    # ==========================================================================
    # <>> COMMANDS
    # ==========================================================================
    def seed_CMD(self, player, cmd, args):

        seed = str(server.seed) if server.seed else 'Random'
        text = MSG['SERVER SEED'].format(seed='<color=lime>%s</color>' % seed)

        self.tell(player, text)

    # --------------------------------------------------------------------------
    def admins_list_CMD(self, player, cmd, args):

        sort = ['<color=cyan>%s</color>' % self.cache[rust.UserIDFromPlayer(i)]['username'] for i in BasePlayer.activePlayerList if i.IsAdmin()]
        sort = [sort[x:x+3] for x in xrange(0, len(sort), 3)]

        if sort:
            self.tell(player, '%s | %s:' % (self.prefix, MSG['ADMINS LIST TITLE']), force=False)
            self.tell(player, LINE, force=False)
            for i in sort:
                self.tell(player, ', '.join(i), 'white', force=False)
            self.tell(player, LINE, force=False)
        else:
            self.tell(player, MSG['NO ADMINS ONLINE'], 'yellow')

    # --------------------------------------------------------------------------
    def plugins_list_CMD(self, player, cmd, args):

        self.tell(player, '%s | %s:' % (self.prefix, MSG['PLUGINS LIST TITLE']), force=False)
        self.tell(player, LINE, force=False)

        for i in plugins.GetAll():
            if i.Author != 'Oxide Team':
                self.tell(player, '<color=lime>{plugin.Title} v{plugin.Version}</color> by {plugin.Author}'.format(plugin=i), force=False)
        self.tell(player, LINE, force=False)

    # --------------------------------------------------------------------------
    def players_list_CMD(self, player, cmd, args):

        l = self.player_list()
        s = BasePlayer.sleepingPlayerList

        pcount = MSG['PLAYERS COUNT'].format(active=str(len(l))) if len(l) > 1 else MSG['ONLY PLAYER']
        pstats = MSG['PLAYERS STATS'].format(sleepers=str(len(s)), total=str(len(l) + len(s)))
        title = '%s | %s:' % (self.prefix, MSG['PLAYERS LIST TITLE'])
        chat = PLUGIN['CHAT PLAYERS LIST']
        console = PLUGIN['CONSOLE PLAYERS LIST']

        if chat:
            names = ['<color=lime>%s</color>' % self.cache[rust.UserIDFromPlayer(x)]['username'] for x in l]
            names = [names[x:x+3] for x in xrange(0, len(names), 3)]
            self.tell(player, title, force=False)
            self.tell(player, LINE, force=False)
            for i in names:
                self.tell(player, ', '.join(i), 'white', force=False)
            self.tell(player, LINE, force=False)
            self.tell(player, pcount, 'yellow', force=False)
            self.tell(player, pstats, 'yellow', force=False)
            if console:
                self.tell(player, '(%s)' % MSG['CHECK CONSOLE NOTE'], 'yellow', force=False)
            self.tell(player, LINE, force=False)
        if console:
            if not chat:
                self.tell(player, '(%s)' % MSG['CHECK CONSOLE NOTE'], 'yellow')
            self.pconsole(player, LINE)
            self.pconsole(player, title)
            self.pconsole(player, LINE)
            for num, ply in enumerate(l):
                self.pconsole(player, '<color=orange>{num}</color> | {steamid} | {country} | <color=lime>{username}</color>'.format(num='%03d' % (num + 1), **self.cache[rust.UserIDFromPlayer(ply)]))
            self.pconsole(player, LINE)
            self.pconsole(player, pcount, 'yellow')
            self.pconsole(player, pstats, 'yellow')
            self.pconsole(player, LINE)

    # --------------------------------------------------------------------------
    def rules_CMD(self, player, cmd, args):

        lang = self.get_lang(player, args[0].upper() if args else None)
        l = self.Config['RULES'][lang]

        if l:
            self.tell(player, '%s | %s:' % (self.prefix, MSG['RULES TITLE']), force=False)
            self.tell(player, LINE, force=False)
            if PLUGIN['RULES LANGUAGE'] != 'AUTO':
                self.tell(player, 'DISPLAYING RULES IN: %s' % PLUGIN['RULES LANGUAGE'], 'yellow', force=False)
            for num, line in enumerate(l):
                self.tell(player, '%s. %s' % (num + 1, line), 'orange', force=False)
            self.tell(player, LINE, force=False)
        else:
            self.tell(player, MSG['NO RULES'], 'yellow')

    # --------------------------------------------------------------------------
    def server_map_CMD(self, player, cmd, args):

        self.tell(player, MSG['SERVER MAP'].format(ip=str(server.ip), port=str(server.port)), 'yellow')

    # --------------------------------------------------------------------------
    def plugin_CMD(self, player, cmd, args):

        self.tell(player, LINE, force=False)
        self.tell(player, '<red>%s<end> <lime>v%s <white>by<end> SkinN<end>' % (self.Title.upper(), self.Version), force=False)
        self.tell(player, self.Description, 'lime', force=False)
        self.tell(player, '| RESOURSE ID: <lime>%s<end> | CONFIG: v<lime>%s<end> |' % (self.ResourceId, self.Config['CONFIG_VERSION']), force=False)
        self.tell(player, LINE, force=False)
        self.tell(player, '<< Click the icon to contact me.', userid='76561197999302614', force=False)

    # ==========================================================================
    # <>> OTHER FUNTIONS
    # ==========================================================================
    def cache_player(self, con):

        steamid = rust.UserIDFromConnection(con)
        name = con.username

        if PLUGIN['ENABLE ADMIN TAGS']:
            if con.authLevel == 1:
                name = '<%s>%s<end> <%s>%s<end>' % (COLOR['MODERATOR TAG'], PLUGIN['MODERATOR TAG'], COLOR['MODERATOR NAME'], name)
            elif con.authLevel == 2:
                name = '<%s>%s<end> <%s>%s<end>' % (COLOR['OWNER TAG'], PLUGIN['OWNER TAG'], COLOR['OWNER NAME'], name)

        self.cache[steamid] = {
            'username': con.username,
            'adminname': name,
            'steamid': steamid,
            'auth': con.authLevel,
            'country': 'Unknown'
        }

    # --------------------------------------------------------------------------
    def player_id(self, target):

        return rust.UserIDFromPlayer(target)

    # --------------------------------------------------------------------------
    def player_list(self):

        return BasePlayer.activePlayerList

    # --------------------------------------------------------------------------
    def get_country(self, player, send=True):

        ip = player.net.connection.ipaddress.split(':')[0]
        country = 'undefined'
        def response_handler(code, response):
            country = response.replace('\n','')
            if country == 'undefined' or code != 200:
                country = 'Unknown'
            steamid = self.player_id(player)
            self.cache[steamid]['country'] = country
            if send:
                if PLUGIN['ENABLE JOIN MESSAGE']:
                    target = self.cache[steamid]
                    if not (PLUGIN['HIDE ADMINS CONNECTIONS'] and int(target['auth']) > 0):
                        if country in self.countries:
                            target['country'] = self.countries[country]
                        self.say(MSG['JOIN MESSAGE'].format(**target), COLOR['JOIN MESSAGE'], steamid)
        webrequests.EnqueueGet('http://ipinfo.io/%s/country' % ip, Action[Int32,String](response_handler), self.Plugin)

    # --------------------------------------------------------------------------
    def get_lang(self, player, force=None):

        default = PLUGIN['RULES LANGUAGE']
        if force:
            if force in self.Config['RULES']:
                return force
            else:
                self.tell(player, MSG['NO LANG'], 'yellow')
                return 'EN'
        elif default == 'AUTO':
            lang = self.cache[self.player_id(player)]['country']
            if lang in ('PT','BR'): lang = 'PT'
            elif lang in ('ES','MX','AR'): lang = 'ES'
            elif lang in ('FR','BE','CH','MC','MU'): lang = 'FR'
            return lang if lang in self.Config['RULES'] else 'EN'
        else:
            return default if default in self.Config['RULES'] else 'EN'

    # ==========================================================================
    # <>> MISC FUNTIONS
    # ==========================================================================
    def SendHelpText(self, player, cmd=None, args=None):

        if PLUGIN['ENABLE HELPTEXT']:
            for cmd in self.cmds:
                i = '%s DESC' % cmd
                if i in MSG:
                    self.tell(player, MSG[i], 'yellow', force=False)

    # --------------------------------------------------------------------------
    def countries_dict(self):

        return {
            'AF': 'Afghanistan',
            'AS': 'American Samoa',
            'AD': 'Andorra',
            'AO': 'Angola',
            'AR': 'Argentina',
            'AU': 'Australia',
            'AT': 'Austria',
            'BE': 'Belgium',
            'BR': 'Brazil',
            'BQ': 'British Antarctic Territory',
            'IO': 'British Indian Ocean Territory',
            'VG': 'British Virgin Islands',
            'BG': 'Bulgaria',
            'CA': 'Canada',
            'CV': 'Cape Verde',
            'CF': 'Central African Republic',
            'TD': 'Chad',
            'CL': 'Chile',
            'CN': 'China',
            'CO': 'Colombia',
            'CR': 'Costa Rica',
            'HR': 'Croatia',
            'CU': 'Cuba',
            'CZ': 'Czech Republic',
            'DK': 'Denmark',
            'DO': 'Dominican Republic',
            'DD': 'East Germany',
            'EC': 'Ecuador',
            'EG': 'Egypt',
            'EE': 'Estonia',
            'FI': 'Finland',
            'FR': 'France',
            'GF': 'French Guiana',
            'PF': 'French Polynesia',
            'TF': 'French Southern Territories',
            'FQ': 'French Southern and Antarctic Territories',
            'GE': 'Georgia',
            'DE': 'Germany',
            'GR': 'Greece',
            'HN': 'Honduras',
            'HU': 'Hungary',
            'IS': 'Iceland',
            'IN': 'India',
            'IE': 'Ireland',
            'IT': 'Italy',
            'JM': 'Jamaica',
            'JP': 'Japan',
            'LU': 'Luxembourg',
            'FX': 'Metropolitan France',
            'MX': 'Mexico',
            'MD': 'Moldova',
            'MC': 'Monaco',
            'ME': 'Montenegro',
            'MA': 'Morocco',
            'MZ': 'Mozambique',
            'NO': 'Norway',
            'PL': 'Poland',
            'PT': 'Portugal',
            'PR': 'Puerto Rico',
            'RO': 'Romania',
            'RU': 'Russia',
            'SG': 'Singapore',
            'SI': 'Slovenia',
            'ZA': 'South Africa',
            'ES': 'Spain',
            'SZ': 'Swaziland',
            'SE': 'Sweden',
            'CH': 'Switzerland',
            'TN': 'Tunisia',
            'TR': 'Turkey',
            'UM': 'U.S. Minor Outlying Islands',
            'PU': 'U.S. Miscellaneous Pacific Islands',
            'VI': 'U.S. Virgin Islands',
            'UG': 'Uganda',
            'UA': 'Ukraine',
            'SU': 'Union of Soviet Socialist Republics',
            'AE': 'United Arab Emirates',
            'GB': 'United Kingdom',
            'US': 'United States'
        }

# ==============================================================================