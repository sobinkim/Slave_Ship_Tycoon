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

        private readonly List<Button> skillButtons = new List<Button>();
        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<Button> libraryButtons = new List<Button>();
        private VisualElement root;
        private CommanderSkillData selectedSkill;
        private int selectedSlot;
        private int builtSlotCount = -1;

        private void OnEnable()
        {
            if (document == null || manager == null)
            { Debug.LogError("CommanderToolkitController의 Document와 Manager를 연결하세요.", this); return; }
            root = document.rootVisualElement;
            root.Q<Button>("autoButton").clicked += ToggleAuto;
            root.Q<Button>("equipSkillButton").clicked += EquipSelected;
            root.Q<Button>("removeSkillButton").clicked += RemoveSelected;
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent += OnLoadout;
            BuildSkills(); RefreshSettings();
        }
        private void Start() { BuildSkills(); RefreshSettings(); }
        private void OnDisable()
        {
            Bus<CommanderSkillLoadoutChangedEvent>.OnEvent -= OnLoadout;
            if (root == null) return;
            root.Q<Button>("autoButton").clicked -= ToggleAuto;
            root.Q<Button>("equipSkillButton").clicked -= EquipSelected;
            root.Q<Button>("removeSkillButton").clicked -= RemoveSelected;
            skillButtons.Clear(); slotButtons.Clear(); libraryButtons.Clear(); root = null;
        }
        private void OnLoadout(CommanderSkillLoadoutChangedEvent evt) { BuildSkills(); RefreshSettings(); }
        private void ToggleAuto() => manager.SetAutoUseEnabled(!manager.IsAutoUseEnabled);

        private static void AddIcon(Button button)
        {
            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("skill-icon"); button.Add(icon);
            var missing = new Label { name = "missingIcon", pickingMode = PickingMode.Ignore };
            missing.AddToClassList("skill-empty-mark"); button.Add(missing);
        }
        private static void SetIcon(Button button, CommanderSkillData skill)
        {
            ShipCardPresentation.SetIcon(button.Q("icon"), skill != null ? skill.Icon : null);
            Label missing = button.Q<Label>("missingIcon");
            missing.text = skill != null ? skill.DisplayName : "+";
            missing.style.fontSize = skill != null ? 22 : 42;
            missing.EnableInClassList("hidden", skill != null && skill.Icon != null);
        }
        private void BuildSkills()
        {
            if (root == null) return;
            skillButtons.Clear(); slotButtons.Clear(); libraryButtons.Clear();
            VisualElement bar = root.Q("skillBar"); bar.Clear();
            VisualElement slots = root.Q("commanderSlots"); slots.Clear();
            VisualElement library = root.Q("commanderRows"); library.Clear();
            builtSlotCount = manager.EquippedSlotCount;
            selectedSlot = Mathf.Clamp(selectedSlot, 0, Mathf.Max(0, builtSlotCount - 1));
            for (int i = 0; i < builtSlotCount; i++)
            {
                int slotIndex = i;
                var skillCell = new VisualElement(); skillCell.AddToClassList("skill-cell");
                var use = new Button(() =>
                {
                    CommanderSkillData skill = manager.GetEquippedSkill(slotIndex);
                    if (skill != null) manager.TryUseSkill(skill.SkillType);
                });
                use.AddToClassList("harbor-button"); use.AddToClassList("command-skill");
                AddIcon(use);
                var cooldown = new Label { name = "cooldown", pickingMode = PickingMode.Ignore };
                cooldown.AddToClassList("skill-cooldown"); use.Add(cooldown);
                var cost = new Label { name = "skillCost", pickingMode = PickingMode.Ignore };
                cost.AddToClassList("skill-cost"); use.Add(cost);
                var track = new VisualElement { pickingMode = PickingMode.Ignore }; track.AddToClassList("skill-charge-track");
                var fill = new VisualElement { name = "chargeFill", pickingMode = PickingMode.Ignore }; fill.AddToClassList("skill-charge-fill");
                track.Add(fill); use.Add(track); skillCell.Add(use);
                var state = new Label { name = "state", pickingMode = PickingMode.Ignore }; state.AddToClassList("skill-state"); skillCell.Add(state);
                bar.Add(skillCell); skillButtons.Add(use);

                var slot = new Button(() =>
                {
                    selectedSlot = slotIndex;
                    CommanderSkillData equipped = manager.GetEquippedSkill(slotIndex);
                    if (equipped != null) selectedSkill = equipped;
                    RefreshSettings();
                });
                slot.AddToClassList("harbor-button"); slot.AddToClassList("loadout-slot"); AddIcon(slot);
                var number = new Label((i + 1).ToString()) { pickingMode = PickingMode.Ignore };
                number.AddToClassList("slot-number"); slot.Add(number);
                var caption = new Label { name = "caption", pickingMode = PickingMode.Ignore }; caption.AddToClassList("slot-caption"); slot.Add(caption);
                slots.Add(slot); slotButtons.Add(slot);
            }
            // The manager's database is the single catalog used by combat and the skill page.
            foreach (CommanderSkillData skill in manager.AvailableSkills)
            {
                if (skill == null) continue;
                CommanderSkillData entry = skill;
                if (selectedSkill == null) selectedSkill = entry;
                var card = new Button(() => { selectedSkill = entry; RefreshSettings(); }) { userData = entry };
                card.AddToClassList("harbor-button"); card.AddToClassList("library-skill"); AddIcon(card); SetIcon(card, entry);
                var name = new Label(entry.DisplayName) { pickingMode = PickingMode.Ignore }; name.AddToClassList("library-name"); card.Add(name);
                var badge = new Label { name = "badge", pickingMode = PickingMode.Ignore }; badge.AddToClassList("library-badge"); card.Add(badge);
                library.Add(card); libraryButtons.Add(card);
            }
        }
        private int EquippedIndex(CommanderSkillData skill)
        {
            if (skill == null) return -1;
            for (int i = 0; i < manager.EquippedSlotCount; i++)
                if (manager.GetEquippedSkill(i) == skill) return i;
            return -1;
        }
        private void EquipSelected()
        {
            if (selectedSkill == null) return;
            bool success = manager.TryEquipSkill(selectedSlot, selectedSkill.SkillType);
            RefreshSettings();
            root.Q<Label>("commanderStatus").text = success
                ? $"{selectedSlot + 1}번에 {selectedSkill.DisplayName} 장착"
                : "이미 장착된 스킬이거나 사용할 수 없는 슬롯입니다.";
        }
        private void RemoveSelected()
        {
            manager.UnequipSkill(selectedSlot);
            RefreshSettings();
            root.Q<Label>("commanderStatus").text = $"{selectedSlot + 1}번 슬롯 해제";
        }
        public void RefreshSettings()
        {
            if (root == null) return;
            for (int i = 0; i < slotButtons.Count; i++)
            {
                CommanderSkillData skill = manager.GetEquippedSkill(i);
                SetIcon(slotButtons[i], skill);
                slotButtons[i].Q<Label>("caption").text = skill != null ? skill.DisplayName : "빈 슬롯";
                slotButtons[i].EnableInClassList("is-selected", i == selectedSlot);
            }
            foreach (Button card in libraryButtons)
            {
                var skill = (CommanderSkillData)card.userData;
                int equipped = EquippedIndex(skill);
                card.Q<Label>("badge").text = equipped >= 0 ? $"{equipped + 1}번 장착 중" : "미장착";
                card.EnableInClassList("is-selected", selectedSkill == skill);
            }
            root.Q<Label>("selectedSkillName").text = selectedSkill != null ? selectedSkill.DisplayName : "등록된 스킬 없음";
            string effect = selectedSkill is TotalOffensiveSkillData offensive
                ? $"아군 전체 공격 속도 +{offensive.AttackSpeedIncreasePercent:0.#}%"
                : selectedSkill != null ? selectedSkill.Description : "아직 사용할 수 있는 스킬이 없습니다.";
            root.Q<Label>("selectedSkillDescription").text = selectedSkill != null
                ? $"{effect}\n지휘 {selectedSkill.GaugeCost:0.#} · 지속 {selectedSkill.Duration:0.#}초 · 재사용 {selectedSkill.Cooldown:0.#}초"
                : effect;
            int equippedIndex = EquippedIndex(selectedSkill);
            Button equip = root.Q<Button>("equipSkillButton");
            equip.text = equippedIndex >= 0 ? $"{equippedIndex + 1}번 장착 중" : $"{selectedSlot + 1}번에 장착";
            equip.SetEnabled(selectedSkill != null && equippedIndex < 0 && builtSlotCount > 0);
            root.Q<Button>("removeSkillButton").SetEnabled(manager.GetEquippedSkill(selectedSlot) != null);
            root.Q<Label>("commanderStatus").text = "장착은 즉시 반영됩니다 · 재사용 대기 유지";
        }
        private void Update()
        {
            if (root == null) return;
            if (builtSlotCount != manager.EquippedSlotCount) { BuildSkills(); RefreshSettings(); }
            root.Q<Label>("gaugeText").text = manager.MaxGauge > 0 ? $"{manager.CurrentGauge:0} / {manager.MaxGauge:0}" : "전투 대기";
            root.Q("gaugeFill").style.width = Length.Percent(manager.MaxGauge > 0 ? Mathf.Clamp01(manager.CurrentGauge / manager.MaxGauge) * 100 : 0);
            Button auto = root.Q<Button>("autoButton");
            auto.text = manager.IsAutoUseEnabled ? "AUTO ON" : "AUTO OFF";
            auto.tooltip = "지휘 스킬 자동 사용";
            auto.EnableInClassList("primary-button", manager.IsAutoUseEnabled);
            for (int i = 0; i < skillButtons.Count; i++)
            {
                Button button = skillButtons[i];
                CommanderSkillData skill = manager.GetEquippedSkill(i);
                SetIcon(button, skill);
                bool hasSkill = skill != null && manager.TryGetRuntimeState(skill.SkillType, out _);
                button.Q<Label>("skillCost").text = hasSkill ? $"{skill.GaugeCost:0}" : "";
                if (!hasSkill)
                {
                    button.SetEnabled(false); button.tooltip = "스킬 탭에서 장착";
                    button.parent.Q<Label>("state").text = "미장착";
                    button.Q<Label>("cooldown").text = "";
                    button.Q("chargeFill").style.width = Length.Percent(0);
                    button.RemoveFromClassList("is-active"); button.RemoveFromClassList("is-ready");
                    continue;
                }
                manager.TryGetRuntimeState(skill.SkillType, out CommanderSkillRuntimeState state);
                bool canUse = manager.CanUseSkill(skill.SkillType);
                float gauge = skill.GaugeCost > 0 ? Mathf.Clamp01(manager.CurrentGauge / skill.GaugeCost) : 1;
                float cooldown = skill.Cooldown > 0 ? 1 - Mathf.Clamp01(state.RemainingCooldown / skill.Cooldown) : 1;
                button.SetEnabled(canUse);
                button.EnableInClassList("is-active", state.IsActive); button.EnableInClassList("is-ready", canUse);
                string status = state.IsActive ? "사용 중"
                    : state.RemainingCooldown > 0 ? "재사용 대기"
                    : !manager.IsBattleRunning ? "전투 대기"
                    : gauge < 1 ? "지휘력 부족" : canUse ? "사용 가능" : "대상 없음";
                button.parent.Q<Label>("state").text = status;
                button.tooltip = $"{skill.DisplayName} · 지휘 {skill.GaugeCost:0} · {status}";
                button.Q<Label>("cooldown").text = state.IsActive ? $"{state.RemainingDuration:0.0}"
                    : state.RemainingCooldown > 0 ? $"{state.RemainingCooldown:0.0}" : "";
                float progress = state.IsActive
                    ? (skill.Duration > 0 ? Mathf.Clamp01(state.RemainingDuration / skill.Duration) : 0)
                    : Mathf.Min(gauge, cooldown);
                button.Q("chargeFill").style.width = Length.Percent(progress * 100);
            }
        }
    }
}
