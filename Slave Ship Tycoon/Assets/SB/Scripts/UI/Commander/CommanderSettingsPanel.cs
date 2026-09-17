using System;
using SB.Core.EventBus;
using SB.Scripts.Commander;
using TMPro;
using UnityEngine;

namespace SB.Scripts
{
    public sealed class CommanderSettingsPanel : MonoBehaviour
    {
        [Serializable] public sealed class SkillRow
        {
            public CommanderSkillData skill;
            public TMP_Text description;
            public TMP_Text actionLabel;
            public UnityEngine.UI.Button equipButton;
        }
        [SerializeField] private CommanderSkillManager manager;
        [SerializeField] private SB.Scripts.Visual.PannelManager panelManager;
        [SerializeField] private GameObject maintenance;
        [SerializeField] private GameObject settings;
        [SerializeField] private UnityEngine.UI.Image maintenanceTab;
        [SerializeField] private UnityEngine.UI.Image settingsTab;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private UnityEngine.UI.Image[] slotSurfaces;
        [SerializeField] private TMP_Text status;
        [SerializeField] private SkillRow[] rows;
        [SerializeField] private Color selectedColor = new Color(.15f,.35f,.35f);
        [SerializeField] private Color normalColor = new Color(.085f,.155f,.19f);
        private int selectedSlot;
        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].equipButton.onClick.AddListener(() => Equip(index));
            }
        }
        private void OnEnable() { Bus<CommanderSkillLoadoutChangedEvent>.OnEvent += OnLoadout; Refresh(); }
        private void OnDisable() => Bus<CommanderSkillLoadoutChangedEvent>.OnEvent -= OnLoadout;
        private void OnLoadout(CommanderSkillLoadoutChangedEvent evt) => Refresh();
        public void ShowMaintenance() { maintenance.SetActive(true); settings.SetActive(false); Refresh(); }
        public void ShowSettings() { maintenance.SetActive(false); settings.SetActive(true); Refresh(); }
        public void OpenSettings() { panelManager.ClickPanelButton(SB.Scripts.Visual.PanelType.Upgrade); ShowSettings(); }
        public void SelectFirst() { selectedSlot=0; Refresh(); }
        public void SelectSecond() { selectedSlot=1; Refresh(); }
        public void RemoveFirst() { manager.UnequipSkill(0); Refresh(); }
        public void RemoveSecond() { manager.UnequipSkill(1); Refresh(); }
        private void Equip(int index)
        {
            bool success = manager.TryEquipSkill(selectedSlot, rows[index].skill.SkillType);
            Refresh();
            status.text = success ? $"{selectedSlot+1}번 슬롯에 {rows[index].skill.DisplayName} 장착" : "이미 장착된 스킬입니다. 기존 슬롯에서 해제하세요.";
        }
        private void Refresh()
        {
            if (manager == null) return;
            maintenanceTab.color=maintenance.activeSelf?selectedColor:normalColor;
            settingsTab.color=settings.activeSelf?selectedColor:normalColor;
            status.text="슬롯 선택 → 스킬 장착 · 즉시 반영, 재사용 대기는 유지";
            for(int i=0;i<slotLabels.Length;i++)
            {
                var skill=manager.GetEquippedSkill(i);
                slotLabels[i].text=$"{i+1}번  ·  {(skill!=null?skill.DisplayName:"빈 슬롯")}";
                slotSurfaces[i].color=i==selectedSlot?selectedColor:normalColor;
            }
            foreach(var row in rows)
            {
                var skill=row.skill;
                string effect=skill is TotalOffensiveSkillData offensive
                    ? $"아군 전체 공격 속도 +{offensive.AttackSpeedIncreasePercent:0.#}%"
                    : skill.Description;
                row.description.text=$"{skill.DisplayName}  ·  {(skill.Category==CommanderSkillCategory.Support?"지원":"공격")}\n{effect}\n지휘력 {skill.GaugeCost:0.#}  ·  지속 {skill.Duration:0.#}초  ·  재사용 {skill.Cooldown:0.#}초";
                int equipped=-1;
                for(int i=0;i<manager.EquippedSlotCount;i++) if(manager.GetEquippedSkill(i)==skill)equipped=i;
                row.actionLabel.text=equipped>=0?$"{equipped+1}번 장착 중":$"{selectedSlot+1}번에 장착";
                row.equipButton.interactable=equipped<0;
            }
        }
    }
}
