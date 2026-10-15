using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SB.Core;
using SB.Scripts;
using SB.Scripts.Projectiles;
public static class VerifyCreatureEmissionV4
{
 public static async Task<string> CaptureCombat()
 {
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  var deadline=DateTime.UtcNow.AddSeconds(12);float peak=0f;bool captured=false;
  string folder=Path.GetFullPath("Artifacts/SeaCreatures");
  while(d.IsRunning&&DateTime.UtcNow<deadline){
   var glowing=d.ActiveCreatures.FirstOrDefault(c=>c.GetCompo<SeaCreatureHitFeedback>().EmissionFlash>0.65f);
   if(glowing!=null){
    var block=new MaterialPropertyBlock();glowing.transform.Find("Visual").GetComponent<SpriteRenderer>().GetPropertyBlock(block);
    peak=block.GetFloat("_HitEmissionFlash");ScreenCapture.CaptureScreenshot(Path.Combine(folder,"CreatureEmissionV4Combat.png"));captured=true;await Task.Delay(150);break;
   }
   await Task.Delay(15);
  }
  await Task.Delay(700);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"CreatureDesignV4Combat.png"));await Task.Delay(150);
  string result="Natural hit emission captured="+captured+" shaderFlash="+peak+" phase="+d.Phase+" wave="+d.Wave;
  File.WriteAllText(Path.Combine(folder,"CreatureEmissionV4Combat.txt"),result);return result;
 }
 public static async Task<string> Main()
 {
  if(!Application.isPlaying)throw new InvalidOperationException("Play required");
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  while(d.IsSailing)await Task.Delay(20);
  var sceneCreatures=d.ActiveCreatures.ToArray();
  foreach(var c in sceneCreatures)c.enabled=false;
  // Stop combat scheduling during a brief presentation probe; normal play resumes on restart.
  d.enabled=false;
  d.RestartEncounter();
  var creatures=d.ActiveCreatures.ToArray();
  foreach(var c in creatures)c.enabled=false;
  var report=new System.Text.StringBuilder();string folder=Path.GetFullPath("Artifacts/SeaCreatures");
  var camera=Camera.main;float viewDepth=Vector3.Dot(Vector3.zero-camera.transform.position,camera.transform.forward);
  float right=camera.ViewportToWorldPoint(new Vector3(1,0.5f,viewDepth)).x;
  foreach(var c in creatures) c.transform.position=new Vector3(Mathf.Min(c.transform.position.x,right-0.8f),c.transform.position.y,0);
  var picked=creatures.GroupBy(c=>c.Kind).Select(g=>g.First()).ToArray();
  await Task.Delay(180);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"CreatureDesignV4.png"));
  foreach(var c in picked){
   var h=c.GetCompo<EntityHealth>();var fx=c.GetCompo<SeaCreatureHitFeedback>();var anchors=c.GetCompo<ShipProjectileAnchorCompo>();
   h.ApplyDamage(0,anchors.HitPosition);h.ApplyHeal(1);await Task.Delay(25);
   bool falseHit=fx.EmissionFlash>0;
   h.ApplyDamage(1,anchors.HitPosition);
   await Task.Delay(35);
   var body=c.transform.Find("Visual").GetComponent<SpriteRenderer>();var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
   report.AppendLine(c.Kind+" falseHit="+falseHit+" flash="+fx.EmissionFlash.ToString("0.000")+" shaderFlash="+block.GetFloat("_HitEmissionFlash").ToString("0.000")+" swimPhasePresent="+block.GetFloat("_SwimPhase"));
  }
  ScreenCapture.CaptureScreenshot(Path.Combine(folder,"CreatureHitEmissionV4.png"));await Task.Delay(220);
  bool faded=picked.All(c=>c.GetCompo<SeaCreatureHitFeedback>().EmissionFlash==0);
  var lethal=picked[0];var lethalHealth=lethal.GetCompo<EntityHealth>();lethalHealth.ApplyDamage(lethalHealth.MaxHealth,lethal.GetCompo<ShipProjectileAnchorCompo>().HitPosition);await Task.Delay(35);
  bool lethalGlows=lethal.IsDead&&lethal.GetCompo<SeaCreatureHitFeedback>().EmissionFlash>0;
  foreach(var c in picked)c.GetCompo<SeaCreatureHitFeedback>().OnDespawnedToPool();
  bool reset=picked.All(c=>c.GetCompo<SeaCreatureHitFeedback>().EmissionFlash==0);
  report.AppendLine("faded="+faded+" lethalEmission="+lethalGlows+" poolReset="+reset);
  foreach(var c in creatures)c.enabled=true;
  d.enabled=true;d.RestartEncounter();
  await Task.Delay(500);
  report.AppendLine("normalRestart="+d.IsRunning+" sprites="+string.Join(",",d.ActiveCreatures.Select(c=>c.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite.name).Distinct()));
  File.WriteAllText(Path.Combine(folder,"CreatureEmissionV4Verification.txt"),report.ToString());return report.ToString();
 }
}
