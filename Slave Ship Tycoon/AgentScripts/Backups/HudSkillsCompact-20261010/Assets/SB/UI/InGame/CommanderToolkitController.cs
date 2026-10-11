using System.Collections.Generic;
using SB.Core.EventBus;
using SB.Scripts.Commander;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.InGame
{
    [DisallowMultipleComponent]
    public sealed class CommanderToolkitController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument document;
        [SerializeField] private CommanderSkillManager manager;
        [SerializeField, Tooltip("기존 지휘 설정 화면에 표시되던 스킬 데이터")] private CommanderSkillData[] availableSkills;

        private readonly List<Button> skillButtons = new List<Button>();
        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> equipButtons = new List<Button>();
        private VisualElement root;
        private int selectedSlot;
        private int builtSlotCount = -1;

        private void OnEnable()
        {
            if (document == null || manager == null) { Debug.LogError("CommanderToolkitController의 참조를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            root.Q<Button>("autoButton").clicked += ToggleAuto;
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent += OnLoadout;
            BuildSkills(); RefreshSettings();
        }
        private void Start() { BuildSkills(); RefreshSettings(); }
        private void OnDisable()
        {
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent -= OnLoadout;
            if (root != null) if (root.Q<Button>("autoButton") is Button autoButtonBinding) autoButtonBinding.clicked -= ToggleAuto;
        }
        private void OnLoadout(CommanderSkillLoadoutChangedEvent evt) { BuildSkills(); RefreshSettings(); }
        private void ToggleAuto() => manager.SetAutoUseEnabled(!manager.IsAutoUseEnabled);

        private void BuildSkills()
        {
            if (root == null) return;
            skillButtons.Clear(); slotButtons.Clear(); equipButtons.Clear();
            VisualElement bar = root.Q("skillBar"); bar.Clear();
            VisualElement slots = root.Q("commanderSlots"); slots.Clear();
            VisualElement rows = root.Q("commanderRows"); rows.Clear();
            builtSlotCount = manager.EquippedSlotCount;
            for (int i = 0; i < builtSlotCount; i++)
            {
                int slotIndex = i;
                var skillButton = new Button(() =>
                {
                    CommanderSkillData skill = manager.GetEquippedSkill(slotIndex);
                    if (skill != null) manager.TryUseSkill(skill.SkillType);
                });
                var skillCell = new VisualElement(); skillCell.AddToClassList("skill-cell");
                skillButton.AddToClassList("harbor-button"); skillButton.AddToClassList("command-skill");
                var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore }; icon.AddToClassList("skill-icon");
                var offensive = new VisualElement { name = "offensiveEmblem", pickingMode = PickingMode.Ignore }; offensive.AddToClassList("skill-emblem");
                for (int chevronIndex = 0; chevronIndex < 2; chevronIndex++)
                {
                    var chevron = new VisualElement { pickingMode = PickingMode.Ignore }; chevron.AddToClassList("emblem-chevron"); offensive.Add(chevron);
                }
                icon.Add(offensive);
                var target = new VisualElement { name = "targetEmblem", pickingMode = PickingMode.Ignore }; target.AddToClassList("emblem-target");
                var center = new VisualElement { pickingMode = PickingMode.Ignore }; center.AddToClassList("emblem-center"); target.Add(center); icon.Add(target);
                var empty = new Label("+") { name = "emptyEmblem", pickingMode = PickingMode.Ignore }; empty.AddToClassList("skill-empty-mark"); icon.Add(empty);
                skillButton.Add(icon);
                var name = new Label { name = "skillName", pickingMode = PickingMode.Ignore }; name.AddToClassList("skill-name"); skillButton.Add(name);
                var cost = new Label { name = "skillCost", pickingMode = PickingMode.Ignore }; cost.AddToClassList("skill-cost"); skillButton.Add(cost);
                var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("skill-charge-track");
                var fill = new VisualElement { name = "chargeFill", pickingMode = PickingMode.Ignore }; fill.AddToClassList("skill-charge-fill"); track.Add(fill); skillButton.Add(track);
                skillCell.Add(skillButton);
                var state = new Label { name = "state", pickingMode = PickingMode.Ignore }; state.AddToClassList("skill-state"); skillCell.Add(state);
                bar.Add(skillCell); skillButtons.Add(skillButton);

                var cell = new VisualElement(); cell.AddToClassList("commander-slot");
                var select = new Button(() => { selectedSlot = slotIndex; RefreshSettings(); }); select.AddToClassList("harbor-button");
                var remove = new Button(() => manager.UnequipSkill(slotIndex)) { text = "해제" }; remove.AddToClassList("harbor-button");
                cell.Add(select); cell.Add(remove); slots.Add(cell); slotButtons.Add(select);
            }
            foreach (CommanderSkillData skill in availableSkills)
            {
                if (skill == null) continue;
                var row = new VisualElement(); row.AddToClassList("commander-row");
                string effect = skill is TotalOffensiveSkillData offensive ? $"아군 전체 공격 속도 +{offensive.AttackSpeedIncreasePercent:0.#}%" : skill.Description;
                var description = new Label($"{skill.DisplayName} · {(skill.Category == CommanderSkillCategory.Support ? "지원" : "공격")}\n{effect}\n지휘력 {skill.GaugeCost:0.#} · 지속 {skill.Duration:0.#}초 · 재사용 {skill.Cooldown:0.#}초");
                description.AddToClassList("commander-description"); row.Add(description);
                var equip = new Button(() =>
                {
                    bool success = manager.TryEquipSkill(selectedSlot, skill.SkillType);
                    RefreshSettings();
                    root.Q<Label>("commanderStatus").text = success ? $"{selectedSlot + 1}번에 {skill.DisplayName} 장착" : "이미 장착된 스킬입니다. 기존 슬롯에서 해제하세요.";
                }) { userData = skill };
                equip.AddToClassList("harbor-button"); row.Add(equip); rows.Add(row); equipButtons.Add(equip);
            }
        }

        public void RefreshSettings()
        {
            if (root == null) return;
            for (int i = 0; i < slotButtons.Count; i++)
            {
                CommanderSkillData skill = manager.GetEquippedSkill(i);
                slotButtons[i].text = $"{i + 1}번 · {(skill != null ? skill.DisplayName : "빈 슬롯")}";
                slotButtons[i].EnableInClassList("is-selected", i == selectedSlot);
                ((Button)slotButtons[i].parent.ElementAt(1)).SetEnabled(skill != null);
            }
            foreach (Button button in equipButtons)
            {
                var skill = (CommanderSkillData)button.userData;
                int equipped = -1;
                for (int i = 0; i < manager.EquippedSlotCount; i++) if (manager.GetEquippedSkill(i) == skill) equipped = i;
                button.text = equipped >= 0 ? $"{equipped + 1}번 장착 중" : $"{selectedSlot + 1}번에 장착";
                button.SetEnabled(equipped < 0 && manager.EquippedSlotCount > 0);
            }
            root.Q<Label>("commanderStatus").text = "슬롯 선택 → 스킬 장착 · 즉시 반영, 재사용 대기는 유지";
        }

        private void Update()
        {
            if (root == null) return;
            if (builtSlotCount != manager.EquippedSlotCount) { BuildSkills(); RefreshSettings(); }
            root.Q<Label>("gaugeText").text = manager.MaxGauge > 0 ? $"{manager.CurrentGauge:0} / {manager.MaxGauge:0}" : "전투 대기";
            root.Q("gaugeFill").style.width = Length.Percent(manager.MaxGauge > 0 ? Mathf.Clamp01(manager.CurrentGauge / manager.MaxGauge) * 100f : 0);
            root.Q<Label>("commandHint").text = manager.IsBattleRunning
                ? $"초당 +{manager.GaugeRecoveryPerSecond:0.#} 회복 · 스킬을 눌러 사용"
                : "전투 중에 지휘 스킬을 사용할 수 있습니다";
            root.Q<Button>("autoButton").text = manager.IsAutoUseEnabled ? "스킬 자동 ON" : "스킬 자동 OFF";
            root.Q<Button>("autoButton").EnableInClassList("primary-button", manager.IsAutoUseEnabled);
            for (int i = 0; i < skillButtons.Count; i++)
            {
                Button button = skillButtons[i];
                VisualElement cell = button.parent;
                CommanderSkillData skill = manager.GetEquippedSkill(i);
                bool hasSkill = skill != null && manager.TryGetRuntimeState(skill.SkillType, out _);
                button.Q<Label>("skillName").text = hasSkill ? skill.DisplayName : "빈 슬롯";
                button.Q<Label>("skillCost").text = hasSkill ? $"지휘 {skill.GaugeCost:0}" : "설정에서 장착";
                ShipCardPresentation.SetIcon(button.Q("icon"), hasSkill ? skill.Icon : null);
                bool needsEmblem = hasSkill && skill.Icon == null;
                button.Q("offensiveEmblem").EnableInClassList("hidden", !needsEmblem || skill.SkillType != CommanderSkillType.TotalOffensive);
                button.Q("targetEmblem").EnableInClassList("hidden", !needsEmblem || skill.SkillType != CommanderSkillType.FocusFire);
                button.Q("emptyEmblem").EnableInClassList("hidden", hasSkill);
                if (!hasSkill)
                {
                    button.SetEnabled(false); cell.Q<Label>("state").text = "미장착";
                    button.Q("chargeFill").style.width = Length.Percent(0);
                    button.RemoveFromClassList("is-active"); button.RemoveFromClassList("is-ready");
                    cell.RemoveFromClassList("is-active"); cell.RemoveFromClassList("is-ready");
                    continue;
                }
                manager.TryGetRuntimeState(skill.SkillType, out CommanderSkillRuntimeState state);
                bool canUse = manager.CanUseSkill(skill.SkillType);
                float gauge = skill.GaugeCost > 0 ? Mathf.Clamp01(manager.CurrentGauge / skill.GaugeCost) : 1;
                float cooldown = skill.Cooldown > 0 ? 1 - Mathf.Clamp01(state.RemainingCooldown / skill.Cooldown) : 1;
                button.SetEnabled(canUse);
                button.EnableInClassList("is-active", state.IsActive); button.EnableInClassList("is-ready", canUse);
                cell.EnableInClassList("is-active", state.IsActive); cell.EnableInClassList("is-ready", canUse);
                cell.Q<Label>("state").text = state.IsActive ? $"사용 중 {state.RemainingDuration:0.0}초"
                    : state.RemainingCooldown > 0 ? $"{state.RemainingCooldown:0.0}초 대기"
                    : !manager.IsBattleRunning ? "전투 대기"
                    : gauge < 1 ? "지휘력 부족"
                    : canUse ? "사용 가능" : "대상 없음";
                float progress = state.IsActive
                    ? (skill.Duration > 0 ? Mathf.Clamp01(state.RemainingDuration / skill.Duration) : 0)
                    : Mathf.Min(gauge, cooldown);
                button.Q("chargeFill").style.width = Length.Percent(progress * 100);
            }
        }
    }
}
