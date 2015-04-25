# ==============================================================================
# CAUSES
# ==============================================================================
# STAB      = KNIFES / SPEARS / PICKAXES / ARROW / ICEPICK / BEAR TRAP
# SLASH     = SALVAGE AXE / HATCHETS / BARRICADES
# BLUNT     = TORCH / ROCK / SALVAGE HAMMER
# BITE      = ANIMALS
# BULLET    = GUNS
# EXPLOSION = C4 / GRENADES
# ==============================================================================
# METABOLISM
# ==============================================================================
# FALL   | DROWNED | POISON | COLD   | HEAT    | RADIATION LEVEL/POISON
# HUNGER | THIRST  | BLEEDING |
# ==============================================================================
# ANIMALS
# ==============================================================================
# HORSE | WOLF | BEAR | BOAR | STAG (Deer) | CHICKEN
# ==============================================================================

import re
import Rust
import BasePlayer
import StringPool
from  UnityEngine import Random
from UnityEngine import Vector3

# GLOBAL VARIABLES
DEV = False
LATEST_CFG = 3.2
LINE = '-' * 50

class deathnotes:

    # ==========================================================================
    # <>> PLUGIN
    # ==========================================================================
    def __init__(self):

        # PLUGIN INFO
        self.Title = 'Death Notes'
        self.Author = 'SkinN'
        self.Description = 'Broadcasts players and animals deaths to chat'
        self.Version = V(2, 4, 4)
        self.ResourceId = 819

    # ==========================================================================
    # <>> CONFIGURATION
    # ==========================================================================
    def LoadDefaultConfig(self):

        # DICTIONARY
        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': self.Title.upper(),
                'BROADCAST TO CONSOLE': True,
                'SHOW SUICIDES': True,
                'SHOW METABOLISM DEATH': True,
                'SHOW EXPLOSION DEATH': True,
                'SHOW TRAP DEATH': True,
                'SHOW BARRICADE DEATH': True,
                'SHOW ANIMAL DEATH': True,
                'SHOW PLAYER KILL': True,
                'SHOW ANIMAL KILL': True,
                'SHOW MESSAGE IN RADIUS': False,
                'MESSAGES RADIUS': 300.00
            },
            'COLORS': {
                'MESSAGE': '#FFFFFF',
                'PREFIX': '#FF0000',
                'ANIMAL': '#00FF00',
                'BODYPART': '#00FF00',
                'WEAPON': '#00FF00',
                'VICTIM': '#00FF00',
                'ATTACKER': '#00FF00',
                'DISTANCE': '#00FF00'
            },
            'MESSAGES': {
                'RADIATION': ('{victim} died from radiation.','{victim} did not know that radiation kills.'),
                'HUNGER': ('{victim} starved to death.','{victim} was a bad hunter, and died of hunger.'),
                'THIRST': ('{victim} died of thirst.','Dehydration has killed {victim}, what a bitch!'),
                'DROWNED': ('{victim} drowned.','{victim} thought he could swim, but guess not.'),
                'COLD': ('{victim} froze to death.','{victim} is an ice cold dead man.'),
                'HEAT': ('{victim} burned to death.','{victim} turned into a human torch.'),
                'FALL': ('{victim} died from a big fall.','{victim} believed he could fly, he believed he could touch the sky!'),
                'BLEEDING': ('{victim} bled to death.','{victim} emptied in blood.'),
                'EXPLOSION': ('{victim} exploded into a million little pieces.','{victim} was a sexy bomb, died from a sexy explosion.'),
                'POISON': ('{victim} died poisoned.','{victim} eat the wrong meat and died poisoned.'),
                'SUICIDE': ('{victim} committed suicide.','{victim} has put an end to his life.'),
                'TRAP': ('{victim} stepped on a snap trap.','{victim} did not watch his steps, died on a trap.'),
                'BARRICADE': ('{victim} died stuck in a barricade.','{victim} trapped into a barricade.'),
                'STAB': ('{attacker} stabbed {victim} to death. (With {weapon}, in the {bodypart})','{attacker} stabbed a {weapon} in {victim}\'s {bodypart}.'),
                'STAB SLEEP': ('{attacker} stabbed {victim} to death, while sleeping. (With {weapon}, in the {bodypart})','{attacker} stabbed {victim}, while sleeping. You sneaky little bastard.'),
                'SLASH': ('{attacker} slashed {victim} into pieces. (With {weapon}, in the {bodypart})','{attacker} has sliced {victim} into a million little pieces.'),
                'SLASH SLEEP': ('{attacker} slashed {victim} into pieces, while sleeping. (With {weapon}, in the {bodypart})','{attacker} killed {victim} with a {weapon}, while sleeping.'),
                'BLUNT': ('{attacker} killed {victim}. (With {weapon}, in the {bodypart})','{attacker} made {victim} die of a {weapon} trauma.'),
                'BLUNT SLEEP': ('{attacker} killed {victim}, while sleeping. (With {weapon}, in the {bodypart})','{attacker} killed {victim} with a {weapon}, while sleeping.'),
                'BULLET': ('{attacker} killed {victim}. (In the {bodypart} with {weapon}, from {distance}m)','{attacker} made {victim} eat some bullets with a {weapon}.'),
                'BULLET SLEEP': ('{attacker} killed {victim}, while sleeping. (In the {bodypart} with {weapon}, from {distance}m)','{attacker} killed {victim} with a {weapon}, while sleeping.'),
                'ARROW': ('{attacker} killed {victim} with an arrow on the {bodypart} from {distance}m','{victim} took an arrow to the knee, and died anyway. (Distance: {distance})'),
                'ARROW SLEEP': ('{attacker} killed {victim} with an arrow on the {bodypart}, while {victim} was asleep.','{attacker} killed {victim} with a {weapon}, while sleeping.'),
                'ANIMAL KILL': ('{victim} killed by a {animal}.','{victim} wasn\'t fast enough and a {animal} caught him.'),
                'ANIMAL KILL SLEEP': ('{victim} killed by a {animal}, while sleeping.','{animal} caught {victim}, while sleeping.'),
                'ANIMAL DEATH': ('{attacker} killed a {animal}. (In the {bodypart} with {weapon}, from {distance}m)',)
            },
            'BODYPARTS': {
                'SPINE': 'Spine',
                'LIP': 'Lips',
                'JAW': 'Jaw',
                'NECK': 'Neck',
                'TAIL': 'Tail',
                'HIP': 'Hip',
                'FOOT': 'Feet',
                'PELVIS': 'Pelvis',
                'LEG': 'Leg',
                'HEAD': 'Head',
                'ARM': 'Arm',
                'JOINT': 'Joint',
                'PENIS': 'Penis',
                'WING': 'Wing',
                'EYE': 'Eye',
                'EAR': 'Ear',
                'CLAVICLE': 'Clavicle',
                'FINGERS': 'Fingers',
                'THIGH': 'Thigh',
                'GROUP': 'Group',
                'LEFT SHOULDER': 'Left Shoulder',
                'RIGHT SHOULDER': 'Right Shoulder',
                'LEFT CALF': 'Left Calf',
                'RIGHT CALF': 'Right Calf',
                'LEFT TOE': 'Left Toe',
                'RIGHT TOE': 'Right Toe',
                'LEFT HAND': 'Left Hand',
                'RIGHT HAND': 'Right Hand',
                'LEFT KNEE': 'Left Knee',
                'RIGHT KNEE': 'Right Knee'
            },
            'WEAPONS': {
                'WOODEN SPEAR': 'Wooden Spear',
                'STONE SPEAR': 'Stone Spear',
                'STONE PICKAXE': 'Stone Pickaxe',
                'HUNTING': 'Hunting Bow',
                'AK47U': 'AK47U',
                'ROCK': 'Rock',
                'HATCHET': 'Hatchet',
                'PICKAXE': 'Pickaxe',
                'BOLTRIFLE': 'Bolt Rifle',
                'SALVAGED HAMMER': 'Salvaged Hammer',
                'SAWNOFFSHOTGUN': 'Sawn-off Shotgun',
                'SALVAGED AXE': 'Salvaged Axe',
                'BONEKNIFE': 'Bone Knife',
                'WATERPIPE': 'Waterpipe Shotgun',
                'HATCHET STONE': 'Stone Hatchet',
                'EOKA': 'Eoka Pistol',
                'SALVAGED ICEPICK': 'Salvaged Icepick',
                'TORCH': 'Torch',
                'THOMPSON': 'Thompson',
                'REVOLVER': 'Revolver'
            },
            'ANIMALS': {
                'STAG': 'Deer',
                'CHICKEN': 'Chicken',
                'WOLF': 'Wolf',
                'BEAR': 'Bear',
                'BOAR': 'Boar',
                'HORSE': 'Horse'
            }
        }

        self.console('Loading default configuration file', True)

    # --------------------------------------------------------------------------
    def UpdateConfig(self):

        # IS OLDER CONFIG TWO VERSIONS OLDER?
        if (self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2) or DEV:

            self.console('Current configuration file is two or more versions older than the latest (Current: v%s / Latest: v%s)' % (self.Config['CONFIG_VERSION'], LATEST_CFG), True)
            
            # RESET CONFIGURATION
            self.Config.clear()

            # LOAD THE DEFAULT CONFIGURATION
            self.LoadDefaultConfig()

        else:

            self.console('Applying new changes to the configuration file (Version: %s)' % LATEST_CFG, True)

            # NEW VERSION VALUE
            self.Config['CONFIG_VERSION'] = LATEST_CFG

            # NEW CHANGES
            self.Config['ANIMALS']['HORSE'] = 'Horse'

        # SAVE CHANGES
        self.SaveConfig()

    # ==========================================================================
    # <>> PLUGIN SPECIFIC
    # ==========================================================================
    def Init(self):

        # UPDATE CONFIG FILE
        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        # CONFIGURATION VARIABLES
        global MSG, PLUGIN, COLOR
        MSG = self.Config['MESSAGES']
        COLOR = self.Config['COLORS']
        PLUGIN = self.Config['SETTINGS']

        # PLUGIN SPECIFIC
        self.prefix = '<color=%s>%s</color>' % (COLOR['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.title = '<color=red>%s</color>' % self.Title.upper()
        self.metabolism = ('DROWNED','HEAT','COLD','THIRST','POISON','HUNGER','RADIATION','BLEEDING','FALL')
        self.fallcache = []

        # COMMANDS
        command.AddChatCommand(self.Title.replace(' ', '').lower(), self.Plugin, 'plugin_CMD')

    # ==========================================================================
    # <>> MESSAGE FUNTIONS
    # ==========================================================================
    def console(self, text, force=False):
        ''' Sends a console message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or force:

            print('[%s v%s] :: %s' % (self.Title, str(self.Version), text))

    # --------------------------------------------------------------------------
    def say(self, text, color='white', userid=0):
        ''' Sends a global chat message '''

        if self.prefix:

            rust.BroadcastChat('%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text), None, str(userid))

        else:

            rust.BroadcastChat('<color=%s>%s</color>' % (color, text), None, str(userid))

    # --------------------------------------------------------------------------
    def tell(self, player, text, color='white', userid=0, force=True):
        ''' Sends a global chat message '''

        if self.prefix and force:

            rust.SendChatMessage(player, '%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, text), None, str(userid))

        else:

            rust.SendChatMessage(player, '<color=%s>%s</color>' % (color, text), None, str(userid))

    # --------------------------------------------------------------------------
    def say_filter(self, text, raw, vpos, attacker):

        color = COLOR['MESSAGE']

        # SEND MESSAGE IN RADIUS?
        if PLUGIN['SHOW MESSAGE IN RADIUS']:

            for player in BasePlayer.activePlayerList:

                if self.get_distance(player.transform.position, vpos) <= float(PLUGIN['MESSAGES RADIUS']):

                    self.tell(player, text, color)

                elif attacker and player == attacker:

                    self.tell(player, text, color)

        # OTHERWISE SEND GLOBAL
        else:

            self.say(text, color)

        if PLUGIN['BROADCAST TO CONSOLE']:

            self.console(raw)

    # ==========================================================================
    # <>> MAIN HOOKS
    # ==========================================================================
    def OnEntityTakeDamage(self, victim, hitinfo):

        if victim.ToPlayer() and str(victim.lastDamage).upper() == 'FALL':

            sid = rust.UserIDFromPlayer(victim)

            if sid not in self.fallcache:

                self.fallcache.append(sid)

    # --------------------------------------------------------------------------
    def OnEntityDeath(self, victim, hitinfo):

        if any(x in str(victim) for x in ('BasePlayer','BaseNPC')):

            # DEATH INFO
            text = None
            attacker = hitinfo.Initiator
            death = str(victim.lastDamage).upper()

            # VICTIM AND ATTACKER POS
            vpos = victim.transform.position
            apos = attacker.transform.position if attacker else vpos

            # ANIMAL NAME
            if not victim.ToPlayer():
                animal = str(victim.LookupPrefabName().split('/')[-1].strip()).upper()
            elif not attacker.ToPlayer():
                animal = str(attacker.LookupPrefabName().split('/')[-1].strip()).upper()
            else:
                animal = None
            animal = self.Config['ANIMALS'][animal] if animal in self.Config['ANIMALS'] else animal

            # WEAPON USED
            if hitinfo.Weapon:
                x = str(hitinfo.Weapon.LookupShortPrefabName()).upper().replace('.WEAPON', '').replace('_', ' ')
                weapon = self.Config['WEAPONS'][x] if x in self.Config['WEAPONS'] else 'None'
            else:
                weapon = 'None'

            # BODYPART
            if hitinfo.HitBone:
                part = StringPool.Get(hitinfo.HitBone)
                part = part.replace('l_', 'left_').replace('r_', 'right_')
                for x in ('spine', 'lip', 'jaw', 'neck', 'tail',\
                          'hip', 'foot', 'pelvis', 'leg', 'arm',\
                          'joint', 'wing', 'ear', 'fingers', 'thigh',\
                          'eye', 'group', 'head', 'clavicle'):
                    if x in part:
                        part = x
                part = part.replace('_', ' ').upper()
            else: part = None
            bodypart = self.Config['BODYPARTS'][part] if part and part in self.Config['BODYPARTS'] else part

            #self.console(LINE)
            #self.console('TYPE: %s' % death)
            #self.console('VICTIM: %s' % victim)
            #self.console('ATTACKER: %s' % attacker)
            #self.console('ANIMAL: %s' % animal)
            #self.console('WEAPON: %s' % weapon)
            #self.console('BODY PART: %s' % bodypart)
            #self.console(LINE)

            # DEATH TYPE MESSAGE
            if 'Player' in str(victim):

                # CHECK IF FALL
                sid = rust.UserIDFromPlayer(victim)

                if death == 'BLEEDING' and sid in self.fallcache:

                    self.fallcache.remove(sid)

                    death = 'FALL'

                # IS PLAYER SLEEPING?
                sleep = victim.IsSleeping()

                if (death == 'SUICIDE' and PLUGIN['SHOW SUICIDES']) or (death in self.metabolism and PLUGIN['SHOW METABOLISM DEATH']):

                    text = death

                if death == 'BITE' and PLUGIN['SHOW ANIMAL KILL']:

                    text = 'ANIMAL KILL' if not sleep else 'ANIMAL KILL SLEEP'

                if death == 'EXPLOSION' and PLUGIN['SHOW EXPLOSION DEATH']:

                    text = death

                if 'BearTrap' in str(attacker) and PLUGIN['SHOW TRAP DEATH']:

                    text = 'TRAP'

                elif 'Barricade' in str(attacker) and PLUGIN['SHOW BARRICADE DEATH']:

                    text = 'BARRICADE'

                elif death in ('SLASH', 'BLUNT', 'STAB', 'BULLET') and PLUGIN['SHOW PLAYER KILL']:

                    if weapon == 'Hunting Bow':

                        text = 'ARROW' if not sleep else 'ARROW SLEEP'

                    elif death in MSG:

                        text = death if not sleep else '%s SLEEP' % death

            elif 'BaseNPC' in str(victim) and attacker.ToPlayer() and PLUGIN['SHOW ANIMAL DEATH']:

                text = 'ANIMAL DEATH'

            # FORMAT MESSAGE
            if text:

                text = MSG[text]

                if isinstance(text, tuple):
                    text = text[Random.Range(0, len(text))]

                d, r = {}, {}

                if victim.ToPlayer():
                    d['victim'] = '<color=%s>%s</color>' % (COLOR['VICTIM'], victim.displayName)
                    r['victim'] = victim.displayName
                elif animal:
                    d['animal'] = '<color=%s>%s</color>' % (COLOR['ANIMAL'], animal)
                    r['animal'] = animal
                if attacker.ToPlayer():
                    d['attacker'] = '<color=%s>%s</color>' % (COLOR['ATTACKER'], attacker.displayName)
                    r['attacker'] = attacker.displayName
                elif animal:
                    d['animal'] = '<color=%s>%s</color>' % (COLOR['ANIMAL'], animal)
                    r['animal'] = animal
                d['weapon'] = '<color=%s>%s</color>' % (COLOR['WEAPON'], weapon)
                r['weapon'] = weapon
                d['bodypart'] = '<color=%s>%s</color>' % (COLOR['BODYPART'], bodypart)
                r['bodypart'] = bodypart
                d['distance'] = '<color=%s>%.2f</color>' % (COLOR['DISTANCE'], self.get_distance(vpos, apos))
                r['distance'] = '%.2f' % self.get_distance(vpos, apos)

                if isinstance(text, str):

                    self.say_filter(text.format(**d), text.format(**r), vpos, attacker)

    # ==========================================================================
    # <>> SIDE FUNTIONS
    # ==========================================================================
    def get_distance(self, p1, p2):
        ''' Returns distance between two positions '''

        return Vector3.Distance(p1, p2)

    # ==========================================================================
    # <>> COMMANDS
    # ==========================================================================
    def plugin_CMD(self, player, cmd, args):

        self.tell(player, LINE, force=False)
        self.tell(player, '<color=lime>%s v%s</color> by <color=lime>SkinN</color>' % (self.title, self.Version), force=False)
        self.tell(player, self.Description, 'lime', force=False)
        self.tell(player, '| RESOURSE ID: <color=lime>%s</color> | CONFIG: v<color=lime>%s</color> |' % (self.ResourceId, self.Config['CONFIG_VERSION']), force=False)
        self.tell(player, LINE, force=False)
        self.tell(player, '<< Click the icon to contact me.', userid='76561197999302614', force=False)

# ==============================================================================