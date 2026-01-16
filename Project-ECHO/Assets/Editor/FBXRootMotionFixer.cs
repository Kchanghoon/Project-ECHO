using UnityEngine;
using UnityEditor;

/// <summary>
/// FBX 파일의 모든 애니메이션 클립에 대해 Root Motion을 Bake Into Pose로 설정하는 에디터 스크립트
/// </summary>
public class FBXRootMotionFixer : EditorWindow
{
    private ModelImporter modelImporter;
    private string fbxPath = "Assets/Resources/01_Anim/BlindHunter_mesh.fbx";

    [MenuItem("Tools/Fix BlindHunter Root Motion")]
    public static void ShowWindow()
    {
        GetWindow<FBXRootMotionFixer>("Root Motion Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("FBX Root Motion 자동 수정", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        fbxPath = EditorGUILayout.TextField("FBX 경로:", fbxPath);

        EditorGUILayout.HelpBox(
            "이 도구는 선택한 FBX의 모든 애니메이션 클립에 대해:\n" +
            "- Root Transform Position (XZ): Bake Into Pose\n" +
            "- Root Transform Position (Y): Bake Into Pose\n" +
            "- Root Transform Rotation: Bake Into Pose\n" +
            "로 설정합니다.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Root Motion 자동 수정", GUILayout.Height(40)))
        {
            FixRootMotion();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 오브젝트에서 FBX 경로 가져오기"))
        {
            if (Selection.activeObject != null)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (path.EndsWith(".fbx") || path.EndsWith(".FBX"))
                {
                    fbxPath = path;
                    Debug.Log($"FBX 경로 설정: {fbxPath}");
                }
                else
                {
                    Debug.LogWarning("선택한 오브젝트가 FBX 파일이 아닙니다.");
                }
            }
        }
    }

    private void FixRootMotion()
    {
        modelImporter = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

        if (modelImporter == null)
        {
            EditorUtility.DisplayDialog("오류",
                $"'{fbxPath}' 경로에서 FBX 파일을 찾을 수 없습니다.\n" +
                "경로를 확인하세요.", "확인");
            return;
        }

        // 현재 클립 설정 가져오기
        ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;

        if (clipAnimations.Length == 0)
        {
            // clipAnimations이 비어있으면 defaultClipAnimations 사용
            clipAnimations = modelImporter.defaultClipAnimations;
        }

        if (clipAnimations.Length == 0)
        {
            EditorUtility.DisplayDialog("오류",
                "FBX에 애니메이션 클립이 없습니다.", "확인");
            return;
        }

        Debug.Log($"총 {clipAnimations.Length}개의 클립 발견");

        // 각 클립에 대해 Root Motion 설정
        for (int i = 0; i < clipAnimations.Length; i++)
        {
            ModelImporterClipAnimation clip = clipAnimations[i];

            Debug.Log($"처리 중: {clip.name}");

            // Root Transform Position (XZ) - 가장 중요!
            clip.lockRootPositionXZ = true;

            // Root Transform Position (Y)
            clip.lockRootHeightY = true;

            // Root Transform Rotation
            clip.lockRootRotation = true;

            // 설정을 keepOriginalPositionXZ = false로 하면 Bake Into Pose와 동일
            clip.keepOriginalPositionXZ = false;
            clip.keepOriginalPositionY = false;
            clip.keepOriginalOrientation = false;

            clipAnimations[i] = clip;
        }

        // 수정된 클립 설정 적용
        modelImporter.clipAnimations = clipAnimations;

        // 변경사항 저장 및 다시 import
        EditorUtility.SetDirty(modelImporter);
        modelImporter.SaveAndReimport();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료!",
            $"{clipAnimations.Length}개의 애니메이션 클립에 Root Motion이 수정되었습니다.\n\n" +
            "이제 게임을 실행해보세요!", "확인");

        Debug.Log($"<color=green>[성공]</color> {clipAnimations.Length}개 클립의 Root Motion 수정 완료!");
    }
}