using SB.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace SB.UI.SeaCreatureTest
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class SeaCreatureTestHUD : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private SeaCreatureSurvivalDirector director;
        private Label waveLabel;
        private Label statusLabel;
        private Label timeLabel;
        private Label killsLabel;
        private Label goldLabel;
        private Label resultLabel;
        private Button pauseButton;
        private Button restartButton;
        private IVisualElementScheduledItem refresh;

        private void OnEnable()
        {
            if (document == null || director == null)
            {
                Debug.LogError("SeaCreatureTestHUD requires its document and survival director.", this);
                return;
            }
            VisualElement root = document.rootVisualElement;
            waveLabel = root.Q<Label>("waveLabel");
            statusLabel = root.Q<Label>("statusLabel");
            timeLabel = root.Q<Label>("timeLabel");
            killsLabel = root.Q<Label>("killsLabel");
            goldLabel = root.Q<Label>("goldLabel");
            resultLabel = root.Q<Label>("resultLabel");
            pauseButton = root.Q<Button>("pauseButton");
            restartButton = root.Q<Button>("restartButton");
            pauseButton.clicked += TogglePause;
            restartButton.clicked += Restart;
            refresh = root.schedule.Execute(Refresh).Every(100);
            Refresh();
        }

        private void OnDisable()
        {
            refresh?.Pause();
            if (pauseButton != null) pauseButton.clicked -= TogglePause;
            if (restartButton != null) restartButton.clicked -= Restart;
        }

        private void TogglePause() => director.TogglePause();
        private void Restart() => director.RestartEncounter();

        private void Refresh()
        {
            waveLabel.text = director.IsSailing ? "항해 중" : $"WAVE {Mathf.Max(1, director.Wave):00}/{director.WavesPerEncounter:00}";
            timeLabel.text = $"항해 {director.DistanceTravelled:N0}m";
            killsLabel.text = $"처치 {director.KillCount:N0}";
            goldLabel.text = $"획득 골드 {director.EarnedGold:N0}";
            statusLabel.text = director.IsPaused ? "일시 정지" : director.IsRunning
                ? director.IsSailing ? $"전방 무리 탐색 중 · {director.EncounterCount:N0}개 무리 격파"
                    : $"함대 정지 · 크리쳐 {director.ActiveCount}마리와 교전 중" : "항해 종료";
            resultLabel.EnableInClassList("hidden", director.IsRunning || director.SpawnedCount == 0);
            pauseButton.text = director.IsPaused ? "계속하기" : "일시 정지";
            pauseButton.SetEnabled(director.IsRunning);
        }
    }
}
