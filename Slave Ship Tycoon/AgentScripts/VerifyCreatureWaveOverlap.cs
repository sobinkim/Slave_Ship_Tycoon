using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SB.Scripts;
using SB.Core;
using SB.Scripts.Projectiles;
public static class VerifyCreatureWaveOverlap
{
 public static async Task<string> CheckHitEffects()
 {
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  var c=d.ActiveCreatures.OrderBy(e=>e.transform.position.x).First();
  var h=c.GetCompo<EntityHealth>();var fx=c.GetCompo<SeaCreatureHitFeedback>();
  var parts=fx.GetComponentsInChildren<ParticleSystem>(true);var anchor=c.GetCompo<ShipProjectileAnchorCompo>();
  h.ApplyDamage(0f,anchor.HitPosition);h.ApplyHeal(5f);
  bool noFalseHits=parts.Sum(p=>p.particleCount)==0;
  h.ApplyDamage(1f,anchor.HitPosition);bool damageEffect=parts.Sum(p=>p.particleCount)>0;
  fx.OnSpawnedFromPool();h.ApplyDamage(h.MaxHealth,anchor.HitPosition);
  bool lethalEffect=c.IsDead&&parts.Sum(p=>p.particleCount)>0;
  d.RestartEncounter();
  var all=UnityEngine.Object.FindObjectsByType<SeaCreatureHitFeedback>(FindObjectsInactive.Include,FindObjectsSortMode.None).SelectMany(e=>e.GetComponentsInChildren<ParticleSystem>(true)).ToArray();
  bool captured=false;var deadline=DateTime.UtcNow.AddSeconds(8);
  while(DateTime.UtcNow<deadline&&d.IsRunning){
   if(all.Sum(p=>p.particleCount)>0){await Task.Delay(40);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts/SeaCreatures/CreatureHitEffect.png"));captured=true;await Task.Delay(150);break;}
   await Task.Delay(25);
  }
  string report="zeroDamageAndHealNoEffect="+noFalseHits+" damageEffect="+damageEffect+" lethalEffect="+lethalEffect+" capture="+captured;
  File.WriteAllText(Path.GetFullPath("Artifacts/SeaCreatures/HitEffectVerification.txt"),report);return report;
 }
 public static async Task<string> Main()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Play required");
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  var effects=UnityEngine.Object.FindObjectsByType<SeaCreatureHitFeedback>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  var particles=effects.SelectMany(e=>e.GetComponentsInChildren<ParticleSystem>(true)).ToArray();
  var report=new StringBuilder();var seen=new HashSet<string>();var previousWave=new Dictionary<SeaCreatureEnemy,int>();
  var overlaps=new List<int>();int lastWave=0,maxParticles=0,maxAlive=0;bool hitCaptured=false;float combatDistance=0;bool stopped=true;
  string folder=Path.GetFullPath("Artifacts/SeaCreatures");var deadline=DateTime.UtcNow.AddSeconds(55);
  while(Application.isPlaying&&d.IsRunning&&DateTime.UtcNow<deadline) {
   int particleCount=particles.Sum(p=>p.particleCount);maxParticles=Math.Max(maxParticles,particleCount);maxAlive=Math.Max(maxAlive,d.ActiveCount);
   if(!hitCaptured&&particleCount>0){hitCaptured=true;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"CreatureHitEffect.png"));}
   if(d.Phase==SeaVoyagePhase.Combat) {
    if(lastWave==0)combatDistance=d.DistanceTravelled;
    stopped&=Mathf.Approximately(combatDistance,d.DistanceTravelled);
    if(d.Wave!=lastWave) {
     int oldAlive=previousWave.Count(pair=>pair.Key.isActiveAndEnabled&&!pair.Key.IsDead&&pair.Key.SpawnGeneration==pair.Value);
     if(lastWave>0)overlaps.Add(oldAlive);
     report.AppendLine("WAVE "+d.Wave+" previousWaveAlive="+oldAlive+" active="+d.ActiveCount+" spawned="+d.CurrentWaveSpawned+" planned="+d.CurrentWavePlanned+" kills="+d.KillCount);
     previousWave.Clear();
     foreach(var c in d.ActiveCreatures){string key=c.GetInstanceID()+":"+c.SpawnGeneration;if(seen.Add(key))previousWave[c]=c.SpawnGeneration;}
     lastWave=d.Wave;
     if(d.Wave==2)ScreenCapture.CaptureScreenshot(Path.Combine(folder,"OverlappingWaves.png"));
    }
   }
   if(d.EncounterCount>=1&&d.IsSailing)break;
   await Task.Delay(35);
  }
  report.AppendLine("RESULT overlaps="+string.Join(",",overlaps)+" allOverlap="+(overlaps.Count==2&&overlaps.All(n=>n>0))+" resumed="+(d.EncounterCount>=1&&d.IsSailing)+" combatStopped="+stopped+" maxAlive="+maxAlive+" peakHitParticles="+maxParticles+" kills="+d.KillCount+" gold="+d.EarnedGold);
  File.WriteAllText(Path.Combine(folder,"WaveOverlapVerification.txt"),report.ToString());return report.ToString();
 }
}
