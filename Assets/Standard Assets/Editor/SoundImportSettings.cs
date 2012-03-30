using UnityEngine;
using UnityEditor;

public class SoundImportSettings : AssetPostprocessor
{
	void OnPreprocessAudio()
	{
		if (assetPath.Contains("sounds")) {
			AudioImporter ai = assetImporter as AudioImporter;
			if (ai) {
				ai.threeD = true;
				ai.loadType = AudioImporterLoadType.StreamFromDisc;
			}
		}
	}
}
