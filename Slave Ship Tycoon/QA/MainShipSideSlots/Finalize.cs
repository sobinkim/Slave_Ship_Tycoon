if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Stop Play Mode first.");
string original = System.IO.File.ReadAllText("QA/MainShipSideSlots/Before/PlayerFleetLoadout.cs.txt").Replace("\r\n","\n");
string staged = System.IO.File.ReadAllText("QA/MainShipSideSlots/PlayerFleetLoadout.cs").Replace("\r\n","\n");
int originalBody = original.IndexOf("        public Ship GetEscortAt");
int stagedBody = staged.IndexOf("        public Ship GetEscortAt");
string contents = staged.Substring(0,stagedBody) + original.Substring(originalBody);
contents = contents.Replace("The third column adds", "The rear column adds");
System.IO.File.WriteAllText("Assets/SB/Scripts/Stage/PlayerFleetLoadout.cs",contents,new System.Text.UTF8Encoding(false));
foreach (string name in new[] { "FormationUI", "FormationUIEquipped", "BattlePositions" })
{
    string from = "Assets/QA/MainShipSideSlots/"+name+".png";
    System.IO.File.Copy(from,"QA/MainShipSideSlots/"+name+".png",true);
}
UnityEditor.AssetDatabase.DeleteAsset("Assets/QA/MainShipSideSlots");
UnityEditor.AssetDatabase.Refresh();
return "Preserved original loadout method bodies; preview images moved outside game assets.";
