using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Sych.ShareAssets.Runtime;

public class CSVWriting : MonoBehaviour
{
    // When the app starts we create a new file (unique per run) in persistentDataPath
    // and then append rows to that file during the session.
    [SerializeField] private Button _shareButton;

    public string fileName = "/test.csv"; // kept for inspector visibility but replaced at runtime
    public string whatTimeIsIt;
    private string _fullFilePath;
    private const string Header = "Find Me Time Average,Find Me Error Count,Matching Card Time,Matching Card Error Count,Word Scramble Time (-1 is a skip),Jigsaw Time,Current Time";

    private void Start()
    {
        // create a unique file per app run using timestamp in persistentDataPath so it's shareable
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = $"scores_{timestamp}.csv";
        _fullFilePath = Path.Combine(Application.persistentDataPath, baseName);

        // write header to new file
        File.WriteAllText(_fullFilePath, Header + Environment.NewLine);

        // initialize the time string
        whatTimeIsIt = DateTime.Now.ToString();

        // hook up share button if provided
        if (_shareButton != null)
            _shareButton.onClick.AddListener(ShareFile);

        Debug.Log($"CSVWriting: created file {_fullFilePath}");

        WriteCSV();
    }

    private void OnDestroy()
    {
        if (_shareButton != null)
            _shareButton.onClick.RemoveListener(ShareFile);
    }

    public void WriteCSV()
    {
        // update timestamp for this row
        whatTimeIsIt = DateTime.Now.ToString();

        // ensure file path exists (should, but be defensive)
        if (string.IsNullOrEmpty(_fullFilePath))
        {
            Debug.LogError("CSVWriting: file path is not set.");
            return;
        }

        // append a new line to the file for this run/session
        var line = string.Join(",",
            Score.findMeTimeAverage.ToString(),
            Score.findMeErrorCount.ToString(),
            Score.matchingCardTimer.ToString(),
            Score.matchingCardErrorCount.ToString(),
            Score.wordScrambleTime.ToString(),
            Score.JigsawTime.ToString(),
            whatTimeIsIt);

        File.AppendAllText(_fullFilePath, line + Environment.NewLine);
        Debug.Log($"CSVWriting: appended row to {_fullFilePath}");
    }

    // Share the created CSV using the same runtime Share API used in ExampleController
    public void ShareFile()
    {
        if (string.IsNullOrEmpty(_fullFilePath) || !File.Exists(_fullFilePath))
        {
            Debug.LogError("CSVWriting: No CSV file to share.");
            return;
        }

        if (!Share.IsPlatformSupported)
        {
            Debug.LogError("CSVWriting: Share platform not supported.");
            return;
        }

        var items = new List<string> { _fullFilePath };
        Debug.Log("CSVWriting: requesting share...");

        Share.Items(items, success =>
        {
            Debug.Log($"CSVWriting: share {(success ? "succeeded" : "failed")}");
        });
    }
}