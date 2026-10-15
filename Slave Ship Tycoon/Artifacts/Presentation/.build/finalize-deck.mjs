import path from 'node:path';
import {pathToFileURL} from 'node:url';
process.env.RUNTIME_NODE_MODULES='C:/Users/sobin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const workspaceDir='C:/Users/sobin/Slave_Ship_Tycoon/Slave Ship Tycoon';
const SKILL_DIR='C:/Users/sobin/.codex/plugins/cache/openai-primary-runtime/presentations/26.1011.11113/skills/presentations';
const buildDir=path.join(workspaceDir,'Artifacts/Presentation/.build');
const {finalizePresentation}=await import(pathToFileURL(path.join(SKILL_DIR,'container_tools/artifact_tool_utils.mjs')).href);
const result=await finalizePresentation({
 workspaceDir,
 candidatePath:path.join(buildDir,'candidate.pptx'),
 finalPath:path.join(workspaceDir,'output/발표자료/게임프로그래밍_발표_30102_김소빈.pptx'),
 explicitTotalSlideCount:4,
 requiredNativeTableOwnerSlides:[],requiredNativeChartOwnerSlides:[],
 pythonExecutable:'C:/Users/sobin/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe',
 integrityValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_package_integrity.py'),
 layoutValidatorPath:path.join(SKILL_DIR,'container_tools/inspect_presentation_layout_geometry.py'),
 layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit'],
 fontPolicy:{basis:'design',families:['Malgun Gothic']},
 verifyArtifactToolImport:true,
 receiptPath:path.join(buildDir,'validation.json'),
});
console.log(JSON.stringify(result));
