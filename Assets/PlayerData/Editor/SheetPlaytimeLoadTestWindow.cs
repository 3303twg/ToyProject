using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class SheetPlaytimeLoadTestWindow : EditorWindow
{
    const int MeanIntervalSeconds = 360;
    const int GameTimeoutSeconds = 20;
    const int DummyGameCount = 5;

    static readonly int[] ConcurrencySteps = { 1, 5, 10, 15, 20, 30, 40 };

    readonly List<Call> inflight = new List<Call>();
    readonly List<Fire> fleetDue = new List<Fire>();
    readonly StringBuilder report = new StringBuilder();

    enum Mode { Burst, Fleet }

    Mode mode;
    bool running;
    int stepIndex;
    int ok;
    int busy;
    int failed;
    int fleetCursor;
    int assumedClients = 1000;
    int observeSeconds = 120;
    double wallStart;
    double callSum;
    double callMax;

    [MenuItem("Tools/플레이타임 부하 테스트")]
    static void Open()
    {
        GetWindow<SheetPlaytimeLoadTestWindow>("플레이타임 부하");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("더미 게임", "부하더미1 ~ 부하더미5");
        EditorGUILayout.HelpBox(
            "1000 클라는 5분에서 7분마다 한 번씩 보내는 상황을 120초만 잘라서 잽니다. 설치 ID는 클라 번호로 고정되고, 응답 대기는 20초입니다. 시트 스크립트를 새 버전으로 배포한 뒤 재야 기기당 한 행인 한도가 나옵니다.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(running))
        {
            if (GUILayout.Button("동시 호출 올리기 (1→40)")) StartBurst();
            assumedClients = EditorGUILayout.IntField("가정 클라 수", assumedClients);
            observeSeconds = EditorGUILayout.IntField("관측 초", observeSeconds);
            if (GUILayout.Button("상시 접속 테스트")) StartFleet();
        }

        if (running)
        {
            string progress = mode == Mode.Fleet
                ? "보낸 " + fleetCursor + " / " + fleetDue.Count + ", 진행 중 " + inflight.Count
                : (stepIndex + 1) + " / " + ConcurrencySteps.Length + " 단계, 진행 중 " + inflight.Count;
            EditorGUILayout.LabelField("진행", progress);
        }

        EditorGUILayout.TextArea(report.ToString(), GUILayout.ExpandHeight(true));
    }

    void StartBurst()
    {
        mode = Mode.Burst;
        report.Length = 0;
        stepIndex = 0;
        inflight.Clear();
        running = true;
        report.AppendLine("동시호출\t성공\t혼잡\t실패\t평균초\t최대초\t초당처리");
        BeginBurstStep();
        EditorApplication.update += Tick;
    }

    void StartFleet()
    {
        mode = Mode.Fleet;
        assumedClients = Mathf.Clamp(assumedClients, 1, 5000);
        observeSeconds = Mathf.Clamp(observeSeconds, 30, 300);
        report.Length = 0;
        inflight.Clear();
        fleetDue.Clear();
        fleetCursor = 0;
        ok = busy = failed = 0;
        callSum = callMax = 0;
        var rng = new System.Random();
        for (var i = 0; i < assumedClients; i++)
        {
            double at = rng.NextDouble() * MeanIntervalSeconds;
            if (at < observeSeconds) fleetDue.Add(new Fire { At = at, Client = i });
        }
        fleetDue.Sort((a, b) => a.At.CompareTo(b.At));
        wallStart = EditorApplication.timeSinceStartup;
        running = true;
        report.AppendLine("가정 클라 " + assumedClients + "대, 평균 간격 " + MeanIntervalSeconds + "초, 관측 " + observeSeconds + "초");
        report.AppendLine("이 구간에 보낼 호출 " + fleetDue.Count);
        report.AppendLine("성공\t혼잡\t실패\t평균초\t최대초\t필요/초\t성공/초\t버티는 클라");
        EditorApplication.update += Tick;
    }

    void BeginBurstStep()
    {
        ok = busy = failed = 0;
        callSum = callMax = 0;
        wallStart = EditorApplication.timeSinceStartup;
        int concurrency = ConcurrencySteps[stepIndex];
        for (var i = 0; i < concurrency; i++)
            inflight.Add(Call.Start(i, DummyGameCount, 30, null));
    }

    void Tick()
    {
        if (mode == Mode.Fleet) TickFleet();
        else TickBurst();
    }

    void TickFleet()
    {
        double elapsed = EditorApplication.timeSinceStartup - wallStart;
        while (fleetCursor < fleetDue.Count && fleetDue[fleetCursor].At <= elapsed)
        {
            var fire = fleetDue[fleetCursor];
            string install = "fleet" + fire.Client.ToString("d4", CultureInfo.InvariantCulture);
            inflight.Add(Call.Start(fire.Client, DummyGameCount, GameTimeoutSeconds, install));
            fleetCursor++;
        }

        CollectFinished();
        if (fleetCursor < fleetDue.Count || inflight.Count > 0)
        {
            Repaint();
            return;
        }

        FinishFleet();
    }

    void FinishFleet()
    {
        running = false;
        EditorApplication.update -= Tick;
        int done = ok + busy + failed;
        double avg = done > 0 ? callSum / done : 0;
        double need = assumedClients / (double)MeanIntervalSeconds;
        double got = ok / (double)observeSeconds;
        double hold = got * MeanIntervalSeconds;
        report.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0}\t{1}\t{2}\t{3:0.00}\t{4:0.00}\t{5:0.00}\t{6:0.000}\t{7:0}",
            ok, busy, failed, avg, callMax, need, got, hold));
        report.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "초당 {0:0.00}건이 필요한데 성공은 초당 {1:0.000}건입니다. 이 배포본이 버티는 규모는 약 {2:0}대입니다.",
            need, got, hold));
        WriteReport("loadtest-fleet-1000.txt");
        Repaint();
    }

    void TickBurst()
    {
        CollectFinished();
        if (inflight.Count > 0) return;

        int concurrency = ConcurrencySteps[stepIndex];
        double wall = Math.Max(0.001, EditorApplication.timeSinceStartup - wallStart);
        double avg = concurrency > 0 ? callSum / concurrency : 0;
        report.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0}\t{1}\t{2}\t{3}\t{4:0.00}\t{5:0.00}\t{6:0.00}",
            concurrency, ok, busy, failed, avg, callMax, ok / wall));
        Repaint();

        bool held = failed == 0 && busy == 0;
        stepIndex++;
        if (!held || stepIndex >= ConcurrencySteps.Length)
        {
            running = false;
            EditorApplication.update -= Tick;
            report.AppendLine(held
                ? "40동시까지 거절 없이 통과했습니다."
                : "거절이 생긴 단계 직전까지가 이 배포본의 동시 처리 한계입니다.");
            WriteReport("loadtest-result.txt");
            Repaint();
            return;
        }

        BeginBurstStep();
    }

    void CollectFinished()
    {
        for (var i = inflight.Count - 1; i >= 0; i--)
        {
            var call = inflight[i];
            if (!call.Request.isDone) continue;
            var seconds = EditorApplication.timeSinceStartup - call.Started;
            var text = call.Request.downloadHandler != null ? call.Request.downloadHandler.text : "";
            if (call.Request.result == UnityWebRequest.Result.Success && text.Contains("\"ok\":true")) ok++;
            else if (text.Contains("busy")) busy++;
            else failed++;
            callSum += seconds;
            if (seconds > callMax) callMax = seconds;
            call.Request.Dispose();
            inflight.RemoveAt(i);
        }
    }

    void WriteReport(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", fileName));
        File.WriteAllText(path, report.ToString(), Encoding.UTF8);
        report.AppendLine(path);
    }

    struct Fire
    {
        public double At;
        public int Client;
    }

    sealed class Call
    {
        public UnityWebRequest Request;
        public double Started;

        public static Call Start(int index, int dummyGames, int timeoutSeconds, string install)
        {
            string game = "부하더미" + ((index % dummyGames) + 1).ToString(CultureInfo.InvariantCulture);
            string session = Guid.NewGuid().ToString("N");
            if (string.IsNullOrEmpty(install)) install = "burst" + session.Substring(0, 12);
            long ended = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string query = SheetPlaytimeConfig.Url
                + "?token=" + UnityWebRequest.EscapeURL(SheetPlaytimeConfig.Token)
                + "&game=" + UnityWebRequest.EscapeURL(game)
                + "&install=" + UnityWebRequest.EscapeURL(install)
                + "&seconds=60"
                + "&session=" + UnityWebRequest.EscapeURL(session)
                + "&ended=" + ended.ToString(CultureInfo.InvariantCulture)
                + "&bench=1";
            var req = UnityWebRequest.Get(query);
            req.timeout = timeoutSeconds;
            req.SendWebRequest();
            return new Call { Request = req, Started = EditorApplication.timeSinceStartup };
        }
    }
}
