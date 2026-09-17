const { chromium } = require('C:/Users/sobin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('fs');
(async()=>{
  const browser = await chromium.launch({headless:true, channel:'msedge'});
  const page = await browser.newPage();
  await page.setContent('<input type="file"><video muted></video>');
  await page.locator('input').setInputFiles('C:/Users/sobin/Downloads/Screen_Recording_20260914_174508_Slayer Legend.mp4');
  const meta=await page.evaluate(async()=>{const v=document.querySelector('video');v.src=URL.createObjectURL(document.querySelector('input').files[0]);await new Promise(r=>v.onloadeddata=r);return {duration:v.duration,width:v.videoWidth,height:v.videoHeight};});
  console.log(meta);
  for (const fraction of [0.1,0.5,0.85]) {
    const png=await page.evaluate(async(t)=>{const v=document.querySelector('video');v.currentTime=t;await new Promise(r=>v.onseeked=r);const c=document.createElement('canvas');c.width=v.videoWidth;c.height=v.videoHeight;c.getContext('2d').drawImage(v,0,0);return c.toDataURL('image/png').split(',')[1];},meta.duration*fraction);
    fs.writeFileSync(`QA/reference-video-${fraction}.png`,Buffer.from(png,'base64'));
  }
  await browser.close();
})();
