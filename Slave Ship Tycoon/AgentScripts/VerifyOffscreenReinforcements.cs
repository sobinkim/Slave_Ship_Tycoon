using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SB.Scripts;
public static class VerifyOffscreenReinforcements
{
 public static async Task<string> Main(){
  if(!Application.isPlaying)throw new InvalidOperationException("Play required");
  var d=UnityEngine.Object.FindFirstObjectByType<SeaCreatureSurvivalDirector>();
  UnityEngine.Random.InitState(1729);d.RestartEncounter();
  var camera=Camera.main;var deadline=DateTime.UtcNow.AddSeconds(42);string folder=Path.GetFullPath("Artifacts/SeaCreatures");
  while(d.IsRunning&&d.Wave<2&&DateTime.UtcNow<deadline)await Task.Delay(20);
  var entrants=d.ActiveCreatures.Where(c=>c.State==SeaCreatureState.Entering).ToArray();
  if(entrants.Length==0)throw new InvalidOperationException("No entering reinforcements observed.");
  float depth=Vector3.Dot(entrants[0].transform.position-camera.transform.position,camera.transform.forward);
  float edge=camera.ViewportToWorldPoint(new Vector3(1,0.5f,depth)).x;
  var renderers=entrants.Select(c=>c.transform.Find("Visual").GetComponent<SpriteRenderer>()).ToArray();
  bool outside=renderers.All(r=>r.bounds.min.x>edge);bool untargetable=entrants.All(c=>!c.IsEngaged);
  int oldAlive=d.ActiveCount-entrants.Length;var first=entrants.OrderBy(c=>c.transform.position.x).First();float startX=first.transform.position.x;
  bool partial=false;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ReinforcementsOutside.png"));
  while(d.IsRunning&&!first.IsEngaged&&DateTime.UtcNow<deadline){
   var body=first.transform.Find("Visual").GetComponent<SpriteRenderer>();
   if(!partial&&body.bounds.min.x<edge){partial=true;ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ReinforcementsEntering.png"));}
   await Task.Delay(20);
  }
  ScreenCapture.CaptureScreenshot(Path.Combine(folder,"ReinforcementsJoined.png"));await Task.Delay(150);
  string report="wave="+d.Wave+" entrants="+entrants.Length+" oldAlive="+oldAlive+" initiallyEntirelyOutside="+outside+" untargetableDuringEntry="+untargetable+" partialEntrySeen="+partial+" enteredAndEngaged="+first.IsEngaged+" deltaX="+(first.transform.position.x-startX).ToString("0.00");
  File.WriteAllText(Path.Combine(folder,"OffscreenReinforcementVerification.txt"),report);return report;
 }
}
