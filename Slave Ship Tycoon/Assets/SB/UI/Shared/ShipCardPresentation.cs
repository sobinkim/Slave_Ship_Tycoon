using SB.Scripts.Fleet;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI
{
    internal static class ShipCardPresentation
    {
        private static readonly string[] GradeClasses =
        {
            "grade-common", "grade-uncommon", "grade-rare", "grade-epic", "grade-legendary"
        };

        public static Button Create(VisualTreeAsset template, VisualElement parent)
        {
            TemplateContainer container = template.Instantiate();
            Button card = container.Q<Button>("shipCard");
            card.RemoveFromHierarchy();
            parent.Add(card);
            return card;
        }

        public static void Bind(Button card, EscortShipData ship, string count, bool selected = false)
        {
            SetIcon(card.Q("shipIcon"), ship != null ? ship.Icon : null);
            card.Q<Label>("shipName").text = ship != null ? ship.DisplayName : "비어 있음";
            card.Q<Label>("shipGrade").text = ship != null ? GradeName(ship.Grade) : string.Empty;
            card.Q<Label>("shipCount").text = count;
            for (int i = 0; i < GradeClasses.Length; i++)
                card.EnableInClassList(GradeClasses[i], ship != null && (int)ship.Grade == i);
            card.EnableInClassList("is-selected", selected);
        }

        public static void SetIcon(VisualElement element, Sprite sprite)
        {
            element.style.backgroundImage = sprite != null
                ? new StyleBackground(sprite)
                : new StyleBackground(StyleKeyword.None);
        }

        public static string GradeName(EscortShipGrade grade) => grade switch
        {
            EscortShipGrade.Common => "일반",
            EscortShipGrade.Uncommon => "고급",
            EscortShipGrade.Rare => "희귀",
            EscortShipGrade.Epic => "영웅",
            EscortShipGrade.Legendary => "전설",
            _ => grade.ToString()
        };
    }
}
