if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
var stage = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.StageSpawnManager>();
var view = UnityEngine.Object.FindFirstObjectByType<SB.Scripts.UI.Fleet.PlayerFleetPanelView>(UnityEngine.FindObjectsInactive.Include);
var stageSO = new UnityEditor.SerializedObject(stage);
var points = stageSO.FindProperty("escortShipSpawnPoints");
var mainPoint = (UnityEngine.Transform)stageSO.FindProperty("mainShipSpawnPoint").objectReferenceValue;
var loadout = (SB.Scripts.PlayerFleetLoadout)stageSO.FindProperty("playerFleetLoadout").objectReferenceValue;
var loadoutPath = UnityEditor.AssetDatabase.GetAssetPath(loadout);
if (!System.IO.File.Exists("QA/MainShipSideSlots/Before/PlayerFleetLoadout.asset.txt"))
    System.IO.File.Copy(loadoutPath, "QA/MainShipSideSlots/Before/PlayerFleetLoadout.asset.txt");
var firstPoint = (UnityEngine.Transform)points.GetArrayElementAtIndex(0).objectReferenceValue;
var lastPoint = (UnityEngine.Transform)points.GetArrayElementAtIndex(4).objectReferenceValue;
points.arraySize = 8;
for (int i = 6; i < 8; i++)
{
    string pointName = i == 6 ? "RearTopSpawnPoint" : "RearBottomSpawnPoint";
    var point = firstPoint.parent.Find(pointName);
    if (point == null) { point = new UnityEngine.GameObject(pointName).transform; point.SetParent(firstPoint.parent, false); }
    point.position = new UnityEngine.Vector3(mainPoint.position.x, i == 6 ? firstPoint.position.y : lastPoint.position.y, mainPoint.position.z);
    points.GetArrayElementAtIndex(i).objectReferenceValue = point;
}
stageSO.ApplyModifiedProperties();
loadout.GetEscortAt(7);
UnityEditor.EditorUtility.SetDirty(loadout);
var manager = (SB.Scripts.Fleet.PlayerFleetManager)stageSO.FindProperty("playerFleetManager").objectReferenceValue;
var managerSO = new UnityEditor.SerializedObject(manager);
managerSO.FindProperty("_initialFormation").arraySize = 8;
managerSO.ApplyModifiedProperties();
var panel = view.transform;
var root = (UnityEngine.RectTransform)panel.Find("FormationRoot");
var viewSO = new UnityEditor.SerializedObject(view);
var slots = viewSO.FindProperty("_formationSlots");
slots.arraySize = 8;
var slotViews = new SB.Scripts.UI.Fleet.FleetFormationSlotView[8];
for (int i = 0; i < 6; i++) slotViews[i] = (SB.Scripts.UI.Fleet.FleetFormationSlotView)slots.GetArrayElementAtIndex(i).objectReferenceValue;
for (int i = 6; i < 8; i++)
{
    var existing = root.Find("FormationSlot_" + (i + 1));
    slotViews[i] = existing != null ? existing.GetComponent<SB.Scripts.UI.Fleet.FleetFormationSlotView>() : UnityEngine.Object.Instantiate(slotViews[0], root);
    slotViews[i].name = "FormationSlot_" + (i + 1);
    slotViews[i].Initialize(i);
    slots.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
}
viewSO.ApplyModifiedProperties();
System.Action<UnityEngine.Transform, float, float, float, float> rect = (t,x,y,w,h) =>
{
    var rt = (UnityEngine.RectTransform)t;
    rt.anchorMin = rt.anchorMax = new UnityEngine.Vector2(0f,1f);
    rt.pivot = new UnityEngine.Vector2(0f,1f);
    rt.anchoredPosition = new UnityEngine.Vector2(x,-y);
    rt.sizeDelta = new UnityEngine.Vector2(w,h);
};
rect(root,44,280,624,568);
var grid = root.GetComponent<UnityEngine.UI.GridLayoutGroup>();
grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
grid.constraintCount = 3;
grid.cellSize = new UnityEngine.Vector2(192,176);
grid.spacing = new UnityEngine.Vector2(24,20);
foreach (var slot in slotViews)
{
    rect(slot.transform.Find("Icon"),21,22,150,100);
    rect(slot.transform.Find("Name"),6,130,180,38);
    var label = slot.transform.Find("Name").GetComponent<TMPro.TMP_Text>();
    label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 20;
    rect(slot.transform.Find("Empty"),0,0,192,176);
    rect(slot.transform.Find("RemoveButton"),144,0,48,48);
}
var fixedCard = root.Find("MainShipFixedSlot");
if (fixedCard == null)
{
    fixedCard = new UnityEngine.GameObject("MainShipFixedSlot",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image)).transform;
    fixedCard.SetParent(root,false);
    var background = fixedCard.GetComponent<UnityEngine.UI.Image>();
    background.color = new UnityEngine.Color(0.24f,0.20f,0.12f,1f);
    background.raycastTarget = false;
    var mainIcon = new UnityEngine.GameObject("Icon",typeof(UnityEngine.RectTransform),typeof(UnityEngine.UI.Image)).transform;
    mainIcon.SetParent(fixedCard,false);
    var icon = mainIcon.GetComponent<UnityEngine.UI.Image>();
    var spriteRenderer = loadout.MainShipPrefab.transform.Find("Visual").GetComponent<UnityEngine.SpriteRenderer>();
    icon.sprite = spriteRenderer.sprite; icon.preserveAspect = true; icon.raycastTarget = false;
    rect(mainIcon,21,22,150,100);
    var name = UnityEngine.Object.Instantiate(slotViews[0].transform.Find("Name").gameObject,fixedCard).transform;
    name.name = "Name"; rect(name,6,130,180,38);
    var nameText = name.GetComponent<TMPro.TMP_Text>();
    nameText.text = "메인 함선 · 고정"; nameText.color = new UnityEngine.Color(1f,0.85f,0.43f); nameText.raycastTarget = false;
}
int[] order = {6,0,1,-1,2,3,7,4,5};
for (int i=0;i<order.Length;i++) (order[i] < 0 ? fixedCard : slotViews[order[i]].transform).SetSiblingIndex(i);
rect(panel.Find("FormationHeading"),44,182,624,44);
panel.Find("FormationHeading").GetComponent<TMPro.TMP_Text>().text = "아군 배치 · 호위선 8칸";
rect(panel.Find("RearColumn"),44,234,192,32);
var middle = panel.Find("MiddleColumn");
if (middle == null) { middle = UnityEngine.Object.Instantiate(panel.Find("RearColumn").gameObject,panel).transform; middle.name = "MiddleColumn"; }
rect(middle,260,234,192,32); middle.GetComponent<TMPro.TMP_Text>().text = "중열";
rect(panel.Find("FrontColumn"),476,234,192,32);
rect(panel.Find("EnemyHeading"),736,182,300,44);
var enemyHeading = panel.Find("EnemyHeading").GetComponent<TMPro.TMP_Text>();
enemyHeading.enableAutoSizing = true; enemyHeading.fontSizeMin=22; enemyHeading.fontSizeMax=28;
rect(panel.Find("AttackDirection"),680,452,48,100);
var preview = panel.Find("AttackPreview");
rect(preview,736,234,300,614);
for (int i=0;i<3;i++)
{
    var column = preview.Find("EnemyColumn"+i); rect(column,i*104,46,92,498);
    foreach (UnityEngine.Transform marker in column) rect(marker,0,44+marker.GetSiblingIndex()*110,92,50);
    var lineLabel = preview.Find("LineLabel"+i); rect(lineLabel,i*104,0,92,32);
    lineLabel.GetComponent<TMPro.TMP_Text>().fontSize = 22;
}
rect(preview.Find("TargetSummary"),0,558,300,48);
var summary = preview.Find("TargetSummary").GetComponent<TMPro.TMP_Text>();
summary.enableAutoSizing=true; summary.fontSizeMin=18; summary.fontSizeMax=22;
panel.Find("FleetHint").GetComponent<TMPro.TMP_Text>().text = "함선 선택 후 배치할 칸을 누르세요 · 메인 함선 위·아래도 배치 가능";
UnityEditor.EditorUtility.SetDirty(view); UnityEditor.EditorUtility.SetDirty(stage); UnityEditor.EditorUtility.SetDirty(manager);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(stage.gameObject.scene);
return "Eight spawn anchors and 3x3 formation UI saved; main ship fixed in rear center.";
