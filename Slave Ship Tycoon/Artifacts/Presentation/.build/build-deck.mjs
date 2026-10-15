import fs from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { createRequire } from 'node:module';

const workspaceDir = 'C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon';
const buildDir = path.join(workspaceDir, 'Artifacts/Presentation/.build');
const outputDir = path.join(workspaceDir, 'output/발표자료');
const runtimeModules = 'C:/Users/sobin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const SKILL_DIR = 'C:/Users/sobin/.codex/plugins/cache/openai-primary-runtime/presentations/26.1011.11113/skills/presentations';
const RUNTIME_PYTHON = 'C:/Users/sobin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';
const { Presentation, PresentationFile } = await import(pathToFileURL(path.join(runtimeModules, '@oai/artifact-tool/dist/artifact_tool.mjs')).href);
const requireRuntime = createRequire(path.join(runtimeModules, '@oai/artifact-tool/package.json'));
const sharp = requireRuntime(path.join(runtimeModules, 'sharp'));
const { finalizePresentation } = await import(pathToFileURL(path.join(SKILL_DIR, 'container_tools/artifact_tool_utils.mjs')).href);

await fs.mkdir(buildDir, {recursive:true});
await fs.mkdir(outputDir, {recursive:true});

const FONT = 'Malgun Gothic';
const C = { bg:'#F4F7F6', dark:'#0B222B', teal:'#087D83', muted:'#587078', white:'#FFFFFF' };
const p = Presentation.create({slideSize:{width:1280,height:720}});

function text(slide, name, value, x, y, width, height, size, color=C.dark, bold=false, align='left') {
  const s = slide.shapes.add({geometry:'textbox',name,position:{left:x,top:y,width,height},fill:'none',line:{fill:'none',width:0}});
  s.text = value;
  s.text.style = {typeface:FONT,fontSize:size,color,bold,alignment:align,verticalAlignment:'middle',wrap:'none',insets:0};
  return s;
}
async function img(slide, file, alt, x,y,width,height) {
  const bytes=await fs.readFile(file);
  return slide.images.add({blob:bytes,contentType:'image/png',alt,fit:'contain',position:{left:x,top:y,width,height}});
}

const battle = path.join(buildDir,'battle-crop.png');
await sharp(path.join(workspaceDir,'Artifacts/Presentation/ingame-current-20261012.png')).extract({left:0,top:230,width:1080,height:750}).png().toFile(battle);

const s1 = p.slides.add();
s1.background.fill = C.bg;
text(s1,'game-name','SLAVE SHIP TYCOON',72,162,550,38,22,C.teal,true);
text(s1,'cover-title','게임프로그래밍\n발표',72,222,550,176,62,C.dark,true);
text(s1,'presenter','30102 김소빈',72,470,500,54,28,C.muted);
await img(s1,battle,'실제 게임의 자동 함대 전투 화면',640,152,568,394.444);
s1.speakerNotes.text = '사진: Unity MCP로 현재 실행 중인 프로젝트의 Game View를 캡처한 뒤 전투 영역을 잘랐습니다. 원본: Artifacts/Presentation/ingame-current-20261012.png.\n게임프로그래밍 발표를 시작하겠습니다.';

const s2 = p.slides.add();
s2.background.fill = C.bg;
text(s2,'overview-title','게임 목적과 플레이 루프',72,50,1120,74,44,C.dark,true);
text(s2,'game-purpose','함대를 키워 더 강한 적을 돌파하고\n화물을 운송·판매하는 방치형 함선 게임',72,148,604,96,27,C.dark);
const steps = [
  ['01','자동 전투·보스 도전'],
  ['02','화물 운송·판매'],
  ['03','함선 모집·합성·강화'],
  ['04','성장한 함대로 다시 도전'],
];
for(const [i,[n,label]] of steps.entries()) {
  const y=300+i*74;
  text(s2,`step-number-${n}`,n,72,y,50,48,27,C.teal,true);
  text(s2,`step-label-${n}`,label,144,y,490,48,27,C.dark,i===3);
}
const transport = path.join(workspaceDir,'Artifacts/Diagnostics/ShipPosition-After-20261011.png');
const recruitment = path.join(workspaceDir,'Artifacts/UI/Recruitment-Compact.png');
await img(s2,transport,'운송 해역의 실제 전투 화면',716,184,232,384.948);
await img(s2,recruitment,'호위선 모집 화면',976,184,232,412.444);
text(s2,'transport-caption','화물 운송',716,611,232,38,23,C.muted,false,'center');
text(s2,'recruitment-caption','호위선 모집',976,611,232,38,23,C.muted,false,'center');
s2.speakerNotes.text = '확정된 큰 흐름: 자동 전투 파밍과 보스 도전, 운송 항로 진행과 정산, 모집·합성·강화·편성 후 다음 파밍으로 복귀합니다.\n기획 출처: Notion 세션 공통 기획 맥락 https://app.notion.com/p/3f52a871b9c1818c9117c73860a030b2\n구현 확인: Docs/UserGuide/01_전체구조와_실행흐름.md, Docs/UserGuide/08_함대편성_모집_합성.md, Docs/UserGuide/12_강화와_자연회복.md, Assets/SB/Scripts/GameFlowManager.cs.\n사진: Artifacts/Diagnostics/ShipPosition-After-20261011.png 및 Artifacts/UI/Recruitment-Compact.png.';

const s3 = p.slides.add();
s3.background.fill = C.dark;
text(s3,'demo-title','실제 시연',72,239,1136,106,76,C.white,true);
text(s3,'demo-transition','지금부터 게임을 직접 실행해 보여드리겠습니다',72,371,1136,58,30,'#B6D3D6');
s3.speakerNotes.text = '이제 실제 게임 화면으로 전환해 자동 전투와 함대 성장을 시연하겠습니다.';

const s4 = p.slides.add();
s4.background.fill = C.bg;
text(s4,'closing','감사합니다',72,260,1136,120,82,C.dark,true,'center');
text(s4,'closing-presenter','30102 김소빈',72,404,1136,54,27,C.muted,false,'center');
s4.speakerNotes.text = '발표를 마치겠습니다. 감사합니다.';

for(const [i,slide] of p.slides.items.entries()) {
  const preview=await p.export({slide,format:'png',scale:1.5});
  await fs.writeFile(path.join(buildDir,`slide-${i+1}.png`),new Uint8Array(await preview.arrayBuffer()));
  const layout=await slide.export({format:'layout'});
  await fs.writeFile(path.join(buildDir,`slide-${i+1}.layout.json`),await layout.text());
}
console.log('Rendered four slides.');
const candidatePath=path.join(buildDir,'candidate.pptx');
await (await PresentationFile.exportPptx(p)).save(candidatePath);
console.log('Exported candidate PPTX.');
const FINAL_PPTX=path.join(outputDir,'게임프로그래밍_발표_30102_김소빈.pptx');
const result=await finalizePresentation({
  workspaceDir,candidatePath,finalPath:FINAL_PPTX,
  explicitTotalSlideCount:4,
  requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],
  pythonExecutable:RUNTIME_PYTHON,
  integrityValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],
  fontPolicy:{basis:'design',families:[FONT]},
  verifyArtifactToolImport:true,
  receiptPath:path.join(buildDir,'validation.json'),
});
console.log(JSON.stringify(result));
