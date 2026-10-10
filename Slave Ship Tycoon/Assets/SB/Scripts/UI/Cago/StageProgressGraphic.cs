using UnityEngine;
using UnityEngine.UI;

namespace SB.Scripts
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StageProgressGraphic : MaskableGraphic
    {
        [SerializeField, Min(4f)] private float diameter = 34f;
        [SerializeField, Min(0f)] private float spacing = 14f;
        [SerializeField, Min(1f)] private float ringWidth = 5f;
        [SerializeField, Range(0f, 1f), Tooltip("진행 표시의 가로 정렬: 0은 왼쪽, 0.5는 중앙, 1은 오른쪽")]
        private float horizontalAlignment = 0.5f;
        [SerializeField] private Color ringColor = new Color(.94f,.97f,.97f);
        [SerializeField] private Color progressColor = new Color(1f,.5f,.16f);
        private int currentStage;
        private int totalStages;
        private const int Segments = 12;

        public void SetProgress(int stage,int total)
        {
            total=Mathf.Max(0,total);
            stage=Mathf.Clamp(stage,0,total);
            if(stage==currentStage && total==totalStages)return;
            currentStage=stage;totalStages=total;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(totalStages<=0)return;
            Rect rect=rectTransform.rect;
            float fit=Mathf.Min(1f,rect.width/(totalStages*diameter+(totalStages-1)*spacing));
            float outer=diameter*.5f*fit;
            float inner=Mathf.Max(0f,outer-ringWidth*fit);
            float progressWidth=(totalStages*diameter+(totalStages-1)*spacing)*fit;
            float startX=rect.xMin+(rect.width-progressWidth)*horizontalAlignment;
            for(int dot=0;dot<totalStages;dot++)
            {
                Vector2 center=new Vector2(startX+outer+dot*(diameter+spacing)*fit,rect.center.y);
                for(int segment=0;segment<Segments;segment++)
                {
                    float angle=2f*Mathf.PI*segment/Segments;
                    float next=2f*Mathf.PI*(segment+1)/Segments;
                    Vector2 a=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Vector2 b=new Vector2(Mathf.Cos(next),Mathf.Sin(next));
                    int start=mesh.currentVertCount;
                    mesh.AddVert(center+a*outer,ringColor,Vector2.zero);mesh.AddVert(center+b*outer,ringColor,Vector2.zero);
                    mesh.AddVert(center+b*inner,ringColor,Vector2.zero);mesh.AddVert(center+a*inner,ringColor,Vector2.zero);
                    mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
                    if(dot>=currentStage)continue;
                    start=mesh.currentVertCount;
                    mesh.AddVert(center,progressColor,Vector2.zero);mesh.AddVert(center+a*inner,progressColor,Vector2.zero);mesh.AddVert(center+b*inner,progressColor,Vector2.zero);
                    mesh.AddTriangle(start,start+1,start+2);
                }
            }
        }
    }
}
