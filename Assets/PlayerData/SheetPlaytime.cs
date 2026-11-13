using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class SheetPlaytimeConfig
{
    public const string Url = "https://script.google.com/macros/s/AKfycbx1RmrpCmNPPKVZh5t7hI5CMMO7z0XY7mvOrEPsICUwbOjxM9bePp7dOt8IE6fzrdmH/exec";
    public const string Token = "v7ihi74hv4ihsklneoi8hcas";
}

public static class SheetPlaytime
{
    const int MinIntervalSeconds = 60;
    const int StartIntervalSeconds = 300;
    const int IntervalStepSeconds = 60;
    const int MaxIntervalSeconds = 900;
    const int JitterSeconds = 30;
    const bool SendInEditor = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        var go = new GameObject(nameof(SheetPlaytime));
        go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Runner>();
    }

    sealed class Runner : MonoBehaviour
    {
        const string InstallKey = "sheet_install_id";

        string installId;
        string statePath;
        string inflightId;
        int inflightSeconds;
        int unsyncedSeconds;
        int intervalSeconds = StartIntervalSeconds;
        bool shortened;
        float unsyncedFraction;
        float nextSendAt;
        bool sending;

        void Awake()
        {
            installId = PlayerPrefs.GetString(InstallKey, "");
            if (string.IsNullOrEmpty(installId))
            {
                installId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(InstallKey, installId);
                PlayerPrefs.Save();
            }

            statePath = Path.Combine(Application.persistentDataPath, "playtime_state.txt");
            LoadState();
            nextSendAt = inflightSeconds > 0 || unsyncedSeconds > 0 ? 5f : intervalSeconds;
            Application.wantsToQuit += OnWantsToQuit;
            StartCoroutine(Loop());
        }

        void OnDestroy()
        {
            SaveState();
            Application.wantsToQuit -= OnWantsToQuit;
        }

        bool OnWantsToQuit()
        {
            SaveState();
            return true;
        }

        void Update()
        {
            unsyncedFraction += Time.unscaledDeltaTime;
            if (unsyncedFraction < 1f) return;
            int whole = Mathf.FloorToInt(unsyncedFraction);
            unsyncedFraction -= whole;
            unsyncedSeconds += whole;
        }

        IEnumerator Loop()
        {
            while (true)
            {
                if (!sending && Time.realtimeSinceStartup >= nextSendAt)
                    yield return SendIfNeeded();
                yield return null;
            }
        }

        IEnumerator SendIfNeeded()
        {
#if UNITY_EDITOR
            if (!SendInEditor)
            {
                nextSendAt = Time.realtimeSinceStartup + NextDelay();
                yield break;
            }
#endif
            if (inflightSeconds <= 0 && unsyncedSeconds > 0)
            {
                inflightId = Guid.NewGuid().ToString("N");
                inflightSeconds = unsyncedSeconds;
                unsyncedSeconds = 0;
            }

            if (inflightSeconds <= 0)
            {
                nextSendAt = Time.realtimeSinceStartup + NextDelay();
                yield break;
            }

            sending = true;
            SaveState();
            bool ok = false;
            yield return SheetPlaytimeRequest.Send(
                Application.productName,
                installId,
                inflightSeconds,
                inflightId,
                false,
                success => ok = success);

            if (ok)
            {
                inflightId = "";
                inflightSeconds = 0;
                if (shortened)
                {
                    intervalSeconds = StartIntervalSeconds;
                    shortened = false;
                }
                else
                {
                    intervalSeconds = Mathf.Min(intervalSeconds + IntervalStepSeconds, MaxIntervalSeconds);
                }
            }
            else
            {
                intervalSeconds = Mathf.Max(intervalSeconds / 2, MinIntervalSeconds);
                shortened = true;
            }

            sending = false;
            SaveState();
            nextSendAt = Time.realtimeSinceStartup + NextDelay();
        }

        int NextDelay()
        {
            return intervalSeconds + UnityEngine.Random.Range(0, JitterSeconds + 1);
        }

        void LoadState()
        {
            inflightId = "";
            inflightSeconds = 0;
            unsyncedSeconds = 0;
            intervalSeconds = StartIntervalSeconds;
            shortened = false;
            if (!File.Exists(statePath)) return;

            var lines = File.ReadAllLines(statePath);
            if (lines.Length > 0) inflightId = lines[0].Trim();
            if (lines.Length > 1) int.TryParse(lines[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out inflightSeconds);
            if (lines.Length > 2) int.TryParse(lines[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out unsyncedSeconds);
            if (lines.Length > 3 && int.TryParse(lines[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int savedInterval))
                intervalSeconds = Mathf.Clamp(savedInterval, MinIntervalSeconds, MaxIntervalSeconds);
            if (lines.Length > 4)
                shortened = lines[4].Trim() == "1";
            if (inflightSeconds < 0) inflightSeconds = 0;
            if (unsyncedSeconds < 0) unsyncedSeconds = 0;
            if (inflightSeconds == 0) inflightId = "";
        }

        void SaveState()
        {
            File.WriteAllText(
                statePath,
                inflightId + "\n" +
                inflightSeconds.ToString(CultureInfo.InvariantCulture) + "\n" +
                unsyncedSeconds.ToString(CultureInfo.InvariantCulture) + "\n" +
                intervalSeconds.ToString(CultureInfo.InvariantCulture) + "\n" +
                (shortened ? "1" : "0") + "\n",
                Encoding.UTF8);
        }
    }
}

public static class SheetPlaytimeRequest
{
    public static IEnumerator Send(string game, string install, int seconds, string session, bool bench, Action<bool> done)
    {
        game = (game ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (string.IsNullOrEmpty(game) || seconds < 0 || !CanSend())
        {
            done(false);
            yield break;
        }

        long ended = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        string query = SheetPlaytimeConfig.Url
            + "?token=" + UnityWebRequest.EscapeURL(SheetPlaytimeConfig.Token)
            + "&game=" + UnityWebRequest.EscapeURL(game)
            + "&install=" + UnityWebRequest.EscapeURL(install)
            + "&seconds=" + seconds.ToString(CultureInfo.InvariantCulture)
            + "&session=" + UnityWebRequest.EscapeURL(session)
            + "&ended=" + ended.ToString(CultureInfo.InvariantCulture)
            + (bench ? "&bench=1" : "");

        using (var req = UnityWebRequest.Get(query))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();
            string text = req.downloadHandler != null ? req.downloadHandler.text : "";
            done(req.result == UnityWebRequest.Result.Success &&
                 (text.Contains("\"ok\":true") || text.Contains("\"ok\": true")));
        }
    }

    public static bool CanSend()
    {
        return SheetPlaytimeConfig.Url.Contains("script.google.com/macros") &&
               SheetPlaytimeConfig.Url.Contains("/exec");
    }
}
