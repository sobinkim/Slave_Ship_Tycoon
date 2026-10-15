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
public static class VerifySeaCreatureContinuity
{
 public static async Task<string> Main()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Play Mode is required");
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729); d.RestartEncounter();
  var ships=UnityEngine.Object.FindObjectsByType<Ship>(FindObjectsSortMode.None).Where(s=>s is not Enemy&&s.isActiveAndEnabled).ToArray();
  var generations=ships.Select(s=>s.SpawnGeneration).ToArray();
  int extraStarts=0,ends=0; Action<BattleStartEvent> onStart=e=>extraStarts++; Action<StageBattleEndedEvent> onEnd=e=>ends++;
  Bus<BattleStartEvent>.OnEvent+=onStart;Bus<StageBattleEndedEvent>.OnEvent+=onEnd;
  var positions=new Dictionary<string,Vector3>();var hits=new Dictionary<string,int>();var charges=new Dictionary<string,int>();
  var sb=new StringBuilder(); float movement=0f;bool warningCaptured=false,combatCaptured=false;
  string dir=Path.GetFullPath("Artifacts/SeaCreatures");Directory.CreateDirectory(dir);
  var deadline=DateTime.UtcNow.AddSeconds(42);
  try {
   while(Application.isPlaying&&d.IsRunning&&d.ElapsedSeconds<17f&&DateTime.UtcNow<deadline){
    foreach(var c in d.ActiveCreatures){string key=c.GetInstanceID()+":"+c.SpawnGeneration;if(!positions.ContainsKey(key))positions[key]=c.transform.position;movement=Math.Max(movement,Vector3.Distance(positions[key],c.transform.position));hits[key]=c.ContactHitCount;charges[key]=c.ChargeCount;}
    if(!warningCaptured&&d.ActiveCreatures.Any(c=>c.State==SeaCreatureState.WindingUp)){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ChargeTelegraph.png"));warningCaptured=true;}
    if(!combatCaptured&&d.ElapsedSeconds>=7f){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ContinuousCombat.png"));combatCaptured=true;sb.AppendLine("AT_7_SECONDS alive="+d.ActiveCount+" kills="+d.KillCount+" gold="+d.EarnedGold);}
    await Task.Delay(150);
   }
   bool sameFleet=ships.Select((s,i)=>s!=null&&s.SpawnGeneration==generations[i]).All(x=>x);
   bool pass=Application.isPlaying&&d.IsRunning&&d.Wave>=2&&d.ActiveCount>0&&extraStarts==0&&ends==0&&sameFleet&&d.KillCount>0&&d.EarnedGold>0;
   sb.AppendLine("CONTINUITY_PASS="+pass+" t="+d.ElapsedSeconds.ToString("0.0")+" wave="+d.Wave+" alive="+d.ActiveCount+" spawned="+d.SpawnedCount+" kills="+d.KillCount+" gold="+d.EarnedGold);
   sb.AppendLine("extraBattleStarts="+extraStarts+" battleEnds="+ends+" sameFleetGenerations="+sameFleet+" maxMovement="+movement.ToString("0.00")+" contactHits="+hits.Values.Sum()+" charges="+charges.Values.Sum());
   foreach(var ship in ships){var h=ship.GetCompo<EntityHealth>();sb.AppendLine(ship.name+" hp="+h.CurrentHealth.ToString("0.0")+"/"+h.MaxHealth+" dead="+ship.IsDead);}
   if(Application.isPlaying){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"Wave2.png"));await Task.Delay(200);}
   File.WriteAllText(Path.Combine(dir,"ContinuityVerification.txt"),sb.ToString());return sb.ToString();
  }finally{Bus<BattleStartEvent>.OnEvent-=onStart;Bus<StageBattleEndedEvent>.OnEvent-=onEnd;}
 }
}
