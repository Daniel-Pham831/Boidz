using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

public class ReportManager : MonoLocator<ReportManager>
{
    private const float RecordingTime_Default = 10f;
    [SerializeField] private float _recordingTime = RecordingTime_Default;

    [Button]
    private async void DoRecordAndSave()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("We can only record FPS in play mode.");
        }

        var recordingTime = _recordingTime != 0f ? _recordingTime : RecordingTime_Default;

        // record total FPS over the recording time then save it to a txt file named "report_{date}_{time}.txt"

        // the file should be saved in the "Report" folder in Application.persistentDataPath
        await RecordAndSave_Internal(recordingTime);
    }

    private async UniTask RecordAndSave_Internal(float recordingTime)
    {
        // Initialize variables
        int frameCount = 0;
        float totalFPS = 0f;

        // Record FPS for the specified duration
        float startTime = Time.time;
        while (Time.time - startTime < recordingTime)
        {
            frameCount++;
            totalFPS += (1f / Time.deltaTime);
            await UniTask.Yield(); // Wait for the next frame
        }

        // Calculate average FPS
        float averageFPS = totalFPS / frameCount;

        // Create the report content
        StringBuilder reportContent = new StringBuilder();
        reportContent.AppendLine("FPS Report");
        reportContent.AppendLine($"Recording Time: {recordingTime} seconds");
        reportContent.AppendLine($"Total Frames: {frameCount}");
        reportContent.AppendLine($"Average FPS: {averageFPS:F2}");

        // Generate file name with current date and time
        string dateTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"report_{dateTime}.txt";

        // Define the directory and file path
        string reportDirectory = Path.Combine(Application.persistentDataPath, "Report");
        if (!Directory.Exists(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        string filePath = Path.Combine(reportDirectory, fileName);

        // Save the report to the file
        await File.WriteAllTextAsync(filePath, reportContent.ToString());

        Debug.Log($"Report saved to: {filePath}");
    }
}