using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SB.Core;
using SB.Core.EventBus;
using SB.Scripts;
using SB.Scripts.AttackCompo;

public static class VerifySeaCreatureV3
{
 public static async Task<string> Preview()
 {
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  string folder=Path.GetFullPath("Artifacts/SeaCreatures");
  await Task.Delay(650);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_Sailing.png"));
  await Task.Delay(5350);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_Combat.png"));
  await Task.Delay(200);return "Final surface art captured; phase="+d.Phase+" wave="+d.Wave;
 }
 public static async Task<string> Main()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Play Mode required");
  var director=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);director.RestartEncounter();
  var camera=Camera.main.transform.parent;
  var ships=UnityEngine.Object.FindObjectsByType<Ship>(FindObjectsSortMode.None).Where(s=>s is not Enemy&&s.isActiveAndEnabled).ToArray();
  var generations=ships.Select(s=>s.SpawnGeneration).ToArray();
  var attacks=ships.Select(s=>s.GetCompo<Base_ShipAttackCompo>()).Where(a=>a!=null).ToArray();
  var first=director.ActiveCreatures.First();float firstX=first.transform.position.x;float cameraX=camera.position.x;float fleetX=ships[0].transform.position.x;
  string folder=Path.GetFullPath("Artifacts/SeaCreatures");Directory.CreateDirectory(folder);
  var report=new StringBuilder();int starts=0,ends=0;Action<BattleStartEvent> start=e=>starts++;Action<StageBattleEndedEvent>end=e=>ends++;
  Bus<BattleStartEvent>.OnEvent+=start;Bus<StageBattleEndedEvent>.OnEvent+=end;
  bool early=false,combat=false,combatShot=false,stopped=true,resumed=false,second=false;int maximumBlocked=0;float combatDistance=0f;int lastWave=0;
  var deadline=DateTime.UtcNow.AddSeconds(55);var initialPhase=director.Phase;int sample=0;
  try {
   while(Application.isPlaying&&director.IsRunning&&DateTime.UtcNow<deadline) {
    maximumBlocked=Math.Max(maximumBlocked,attacks.Count(a=>a.IsFireLaneBlocked));
    if(!early&&director.ElapsedSeconds>=0.5f&&director.IsSailing){early=true;report.AppendLine("SAILING worldCreatureDeltaX="+(first.transform.position.x-firstX).ToString("0.000")+" fleetDeltaX="+(ships[0].transform.position.x-fleetX).ToString("0.000")+" cameraDeltaX="+(camera.position.x-cameraX).ToString("0.000")+" targets="+attacks.Count(a=>a.CurrentTarget!=null)+" leftFacing="+(first.GetComponentInChildren<SpriteRenderer>().flipX==false));ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_Sailing.png"));}
    if(director.Phase!=initialPhase){report.AppendLine("PHASE "+initialPhase+" → "+director.Phase+" at "+director.ElapsedSeconds.ToString("0.0")+"s distance="+director.DistanceTravelled.ToString("0.00"));initialPhase=director.Phase;}
    if(director.Phase==SeaVoyagePhase.Combat) {
     if(!combat){combat=true;combatDistance=director.DistanceTravelled;}
     if(director.EncounterCount==0)stopped&=Mathf.Approximately(combatDistance,director.DistanceTravelled);
     if(director.Wave!=lastWave){lastWave=director.Wave;report.AppendLine("WAVE "+lastWave+" alive="+director.ActiveCount+" kills="+director.KillCount);}
     if(!combatShot&&director.ElapsedSeconds>4f){combatShot=true;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_Combat.png"));}
     if(director.EncounterCount>=1){second=true;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_SecondEncounter.png"));break;}
    }
    if(director.EncounterCount>=1&&director.IsSailing&&!resumed){resumed=true;report.AppendLine("RESUMED sailing after encounter kills="+director.KillCount);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"V3_Resume.png"));}
    if(++sample%50==0)File.WriteAllText(Path.Combine(folder,"V3_LiveProgress.txt"),"time="+director.ElapsedSeconds+" phase="+director.Phase+" wave="+director.Wave+" alive="+director.ActiveCount+" kills="+director.KillCount);
    await Task.Delay(100);
   }
   bool sameFleet=ships.Select((s,i)=>s!=null&&s.SpawnGeneration==generations[i]).All(x=>x);
   report.AppendLine("RESULT stoppedDuringCombat="+stopped+" resumed="+resumed+" secondEncounter="+second+" maximumBlockedShooters="+maximumBlocked+" extraBattleStarts="+starts+" battleEnds="+ends+" sameFleet="+sameFleet+" kills="+director.KillCount+" gold="+director.EarnedGold);
   foreach(var ship in ships){var h=ship.GetCompo<EntityHealth>();report.AppendLine(ship.name+" hp="+h.CurrentHealth+" dead="+ship.IsDead);}
   float pauseDistance=director.DistanceTravelled;director.TogglePause();await Task.Delay(250);report.AppendLine("PAUSE distanceStable="+Mathf.Approximately(pauseDistance,director.DistanceTravelled));director.TogglePause();
   File.WriteAllText(Path.Combine(folder,"V3_Verification.txt"),report.ToString());return report.ToString();
  }finally{Bus<BattleStartEvent>.OnEvent-=start;Bus<StageBattleEndedEvent>.OnEvent-=end;}
 }
}
