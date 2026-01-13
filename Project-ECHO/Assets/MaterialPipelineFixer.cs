// Assets/Editor/MaterialPipelineFixer.cs
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class MaterialPipelineFixer
{
    [MenuItem("Tools/Render Pipeline/Fix Pink Materials (Auto)")]
    public static void FixPinkMaterialsAuto()
    {
        // 현재 파이프라인 감지
        var rp = GraphicsSettings.currentRenderPipeline;
        bool isSRP = rp != null;
        bool isHDRP = isSRP && rp.GetType().Name.Contains("HDRenderPipelineAsset");
        bool isURP = isSRP && !isHDRP; // 대략적 구분(URP/기타 SRP)

        string targetShaderName =
            isHDRP ? "HDRP/Lit" :
            isURP ? "Universal Render Pipeline/Lit" :
                     "Standard";

        var targetShader = Shader.Find(targetShaderName);
        if (targetShader == null)
        {
            Debug.LogError($"대상 셰이더를 찾지 못했습니다: {targetShaderName}");
            return;
        }

        // 프로젝트 내 모든 Material 검색
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int changed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // 핑크 원인: 셰이더가 없거나/미지원(=mat.shader null) 또는 error shader 느낌일 때
                // 여기는 "전체 일괄"이 목적이라, 필요하면 조건을 강화해도 됩니다.
                bool needsFix = (mat.shader == null) || mat.shader.name.Contains("InternalErrorShader");

                // Standard인데 URP/HDRP에서 쓰고 있으면 대부분 핑크/비정상 가능 → 교체 대상으로 포함
                if (!needsFix && isSRP && mat.shader != null && mat.shader.name == "Standard")
                    needsFix = true;

                if (!needsFix) continue;

                Undo.RecordObject(mat, "Fix Material Pipeline");

                // 기존 텍스처 백업(공통적으로 존재하는 경우가 많음)
                var oldMainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                var oldColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                var oldBump = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

                // MetallicGloss는 Standard에서 흔함
                var oldMetalGloss = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
                float oldMetallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float oldSmooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

                // Occlusion
                var oldOcc = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;

                // Emission
                var oldEmission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                var oldEmissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

                // 셰이더 교체
                mat.shader = targetShader;

                // 파이프라인별 기본 매핑
                if (isHDRP)
                {
                    // HDRP/Lit
                    // BaseColorMap: _BaseColorMap, BaseColor: _BaseColor
                    if (mat.HasProperty("_BaseColorMap") && oldMainTex != null) mat.SetTexture("_BaseColorMap", oldMainTex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oldColor);

                    if (mat.HasProperty("_NormalMap") && oldBump != null) mat.SetTexture("_NormalMap", oldBump);

                    // MaskMap: HDRP는 (Metallic, AO, DetailMask, Smoothness) packed 텍스처를 쓰는 경우가 많아
                    // Standard의 MetallicGloss를 그대로 넣어도 완벽하진 않음. 우선 넣어두고 필요 시 후처리 권장.
                    if (mat.HasProperty("_MaskMap") && oldMetalGloss != null) mat.SetTexture("_MaskMap", oldMetalGloss);

                    // Emission
                    if (mat.HasProperty("_EmissiveColorMap") && oldEmission != null) mat.SetTexture("_EmissiveColorMap", oldEmission);
                    if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", oldEmissionColor);
                }
                else if (isURP)
                {
                    // URP/Lit
                    // BaseMap: _BaseMap, BaseColor: _BaseColor
                    if (mat.HasProperty("_BaseMap") && oldMainTex != null) mat.SetTexture("_BaseMap", oldMainTex);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oldColor);

                    // Normal
                    if (mat.HasProperty("_BumpMap") && oldBump != null)
                    {
                        mat.SetTexture("_BumpMap", oldBump);
                        // 노멀맵 키워드 활성화
                        mat.EnableKeyword("_NORMALMAP");
                    }

                    // Metallic/Smoothness
                    if (mat.HasProperty("_MetallicGlossMap") && oldMetalGloss != null)
                    {
                        mat.SetTexture("_MetallicGlossMap", oldMetalGloss);
                        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    }
                    if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", oldMetallic);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", oldSmooth);

                    // Occlusion
                    if (mat.HasProperty("_OcclusionMap") && oldOcc != null) mat.SetTexture("_OcclusionMap", oldOcc);

                    // Emission
                    if (mat.HasProperty("_EmissionMap") && oldEmission != null) mat.SetTexture("_EmissionMap", oldEmission);
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", oldEmissionColor);
                    if (oldEmission != null || oldEmissionColor.maxColorComponent > 0.001f)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                }
                else
                {
                    // Built-in(Standard)로 회귀하는 경우
                    if (mat.HasProperty("_MainTex") && oldMainTex != null) mat.SetTexture("_MainTex", oldMainTex);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", oldColor);
                }

                EditorUtility.SetDirty(mat);
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"완료: {changed}개 머티리얼을 '{targetShaderName}' 기준으로 수정했습니다.");
    }
}
