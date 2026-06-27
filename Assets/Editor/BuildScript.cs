using UnityEditor;
using UnityEngine;

public static class BuildScript
{
	public static void PerformBuild()
	{
		string[] scenes = { "Assets/Scenes/MainScene.unity" };
		string buildPath = "build/Android/Android.apk";

		BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.Android, BuildOptions.None);
		Debug.Log("Build completed successfully!");
	}
}