using UnityEditor;
using UnityEngine;

public static class BuildScript
{
	public static void PerformBuild()
	{
		Debug.Log("=== Starting Android Build ===");

		string[] scenes = { "Assets/Scenes/MainScene.unity" };
		string buildPath = "build/Android/Android.apk";

		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = buildPath,
			target = BuildTarget.Android,
			options = BuildOptions.None
		};

		BuildPipeline.BuildPlayer(options);
		Debug.Log("=== Build completed successfully ===");
	}
}