using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Twig Remover", "bawNg", 0.2)]
    class TwigRemover : RustPlugin
    {
        const float cupboardDistance = 60f;
        
        [ConsoleCommand("twigs.count")]
        void cmdCountTwigs(ConsoleSystem.Arg arg)
        {
            if (arg.Player() && !arg.Player().IsAdmin())
            {
                SendReply(arg, "You need to be admin to use that command");
                return;
            }
            var twig_blocks = FindAllCupboardlessTwigBlocks();
            SendReply(arg, $"There are {twig_blocks.Count} twig blocks outside of cupboard range");
        }

        [ConsoleCommand("twigs.remove")]
        void cmdRemoveTwigs(ConsoleSystem.Arg arg)
        {
            if (arg.Player() && !arg.Player().IsAdmin())
            {
                SendReply(arg, "You need to be admin to use that command");
                return;
            }
            PrintToChat("<color=red>Admin is removing all twig blocks outside of cupboard range...</color>");
            var twig_blocks = FindAllCupboardlessTwigBlocks();
            var started_at = Time.realtimeSinceStartup;
            foreach (var building_block in twig_blocks)
                building_block.Kill();
            Puts($"[TwigRemover] Destroyed {twig_blocks.Count} twig blocks in {Time.realtimeSinceStartup - started_at:0.000} seconds");
            PrintToChat($"<color=yellow>Admin has removed {twig_blocks.Count} twig blocks from the map</color>");
        }

        HashSet<BuildingBlock> FindAllCupboardlessTwigBlocks()
        {
            var tool_cupboards = FindAllToolCupboards();
            var twig_blocks = FindAllTwigBuildingBlocks();                        
            var started_at = Time.realtimeSinceStartup;
            Puts($"[TwigRemover] Checking {twig_blocks.Count} twig blocks against {tool_cupboards.Length} tool cupboards...");
            foreach (var cupboard in tool_cupboards)
            {
                foreach (var collider in Physics.OverlapSphere(cupboard.transform.position, cupboardDistance))
                {
                    var building_block = collider.GetComponentInParent<BuildingBlock>();
                    if (building_block) twig_blocks.Remove(building_block);
                }
            }
            Puts($"[TwigRemover] Finding {twig_blocks.Count} cupboardless twig blocks took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return twig_blocks;
        }
        
        HashSet<BuildingBlock> FindAllTwigBuildingBlocks()
        {
            var started_at = Time.realtimeSinceStartup;
            Puts("[TwigRemover] Finding all twig blocks...");
            var building_blocks = UnityEngine.Object.FindObjectsOfType<BuildingBlock>();
            var twig_blocks = new HashSet<BuildingBlock>(building_blocks.Where(block => block.grade == BuildingGrade.Enum.Twigs));
            Puts($"[TwigRemover] Finding {twig_blocks.Count} twig blocks took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return twig_blocks;
        }

        BaseCombatEntity[] FindAllToolCupboards()
        {
            var started_at = Time.realtimeSinceStartup;
            Puts("[TwigRemover] Finding all tool cupboards...");
            var combat_entities = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>();
            var tool_cupboards = combat_entities.Where(entity => entity.LookupPrefabName() == "items/cupboard.tool.deployed").ToArray();
            Puts($"[TwigRemover] Finding {tool_cupboards.Length} tool cupboards took {Time.realtimeSinceStartup - started_at:0.000} seconds");
            return tool_cupboards;
        }
    }
}