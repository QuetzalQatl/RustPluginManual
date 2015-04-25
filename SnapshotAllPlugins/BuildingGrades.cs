using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Building Grades", "bawNg", 0.1)]
    class BuildingGrades : RustPlugin
    {
        FieldInfo serverInputField = typeof(BasePlayer).GetField("serverInput", BindingFlags.Instance | BindingFlags.NonPublic);

        [ChatCommand("up")]
        void UpCommand(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, true);
        }

        [ChatCommand("down")]
        void DownCommand(BasePlayer player, string command, string[] args)
        {
            ChangeBuildingGrade(player, false);
        }

        void ChangeBuildingGrade(BasePlayer player, bool increment)
        {
            if (!player.IsAdmin())
            {
                SendReply(player, "<color=red>Only admins may use that command</color>");
                return;
            }

            var initial_block = GetTargetBuildingBlock(player);
            if (!initial_block)
            {
                SendReply(player, "<color=red>You are not looking at a building block!</color>");
                return;
            }
            
            var all_blocks = new HashSet<BuildingBlock>();
            all_blocks.Add(initial_block);

            Action<BuildingBlock> queue_attached_blocks = null;
            queue_attached_blocks = (building_block) =>
            {
                foreach (var collider in Physics.OverlapSphere(building_block.transform.position, 3.5f))
                {
                    var next_block = collider.GetComponentInParent<BuildingBlock>();
                    if (!next_block || !all_blocks.Add(next_block)) continue;
                    queue_attached_blocks(next_block);
                }
            };
            queue_attached_blocks(initial_block);
            
            foreach (var building_block in all_blocks)
            {
                var target_grade = NextBlockGrade(building_block, increment ? 1 : -1);
                if (target_grade == (int)building_block.grade) continue;
                                
                building_block.SetGrade((BuildingGrade.Enum)target_grade);
                building_block.SetHealthToMax();
                building_block.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }            
        }

        int NextBlockGrade(BuildingBlock building_block, int offset = 1)
        {
            var current_grade = (int)building_block.grade;

            var grades = building_block.blockDefinition.grades;
            if (grades == null) return current_grade;

            var target_grade = current_grade + offset;
            while (target_grade >= 0 && target_grade < grades.Length)
            {
                if (grades[target_grade] != null) return target_grade;
                target_grade += offset;
            }

            return current_grade;
        }

        BuildingBlock GetTargetBuildingBlock(BasePlayer player)
        {
            var eye_postion = player.transform.position;
            eye_postion.y += 1.51f;
            var input = serverInputField.GetValue(player) as InputState;
            var direction = Quaternion.Euler(input.current.aimAngles);
            RaycastHit initial_hit;
            if (!Physics.Raycast(new Ray(eye_postion, direction * Vector3.forward), out initial_hit, 150f) || initial_hit.collider is TerrainCollider)
                return null;
            var entity = initial_hit.collider.GetComponentInParent<BuildingBlock>();
            if (!entity) return null;
            return entity;
        }
    }
}
