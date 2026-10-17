using UnityEngine;

namespace ProjectSS.Expedition
{
    [CreateAssetMenu(menuName = "ProjectSS/Skill", fileName = "NewSkill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        public Sprite icon;
        // 'enabled' belongs to the old inline format and is not used by shared skills.
        public GearSkillDefinition settings = new GearSkillDefinition { enabled = true };
    }
}
