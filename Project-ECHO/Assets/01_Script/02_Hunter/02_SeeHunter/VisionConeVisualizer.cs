using UnityEngine;

/// <summary>
/// SeeHunter의 시야 범위를 원뿔 형태로 시각화하는 컴포넌트
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionConeVisualizer : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 12f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private int resolution = 30; // 원뿔의 해상도 (높을수록 부드러움)
    [SerializeField] private float height = 0.2f; // 시야 범위의 높이

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(1f, 0f, 0f, 0.2f); // 반투명 빨강
    [SerializeField] private Color detectedColor = new Color(1f, 0.5f, 0f, 0.4f); // 감지 시 주황색
    [SerializeField] private Material visionMaterial;

    private Mesh visionMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private SeeHunterAI hunterAI;
    private bool isPlayerDetected = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        hunterAI = GetComponentInParent<SeeHunterAI>();

        // 메시 생성
        visionMesh = new Mesh();
        visionMesh.name = "Vision Cone";
        meshFilter.mesh = visionMesh;

        // 머티리얼 설정
        if (visionMaterial == null)
        {
            visionMaterial = CreateVisionMaterial();
        }
        meshRenderer.material = visionMaterial;

        // 그림자 비활성화
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void Start()
    {
        // 초기 메시 생성
        GenerateVisionMesh();
    }

    private void LateUpdate()
    {
        // 매 프레임 메시 업데이트 (장애물 체크 포함)
        GenerateVisionMesh();

        // 플레이어 감지 상태에 따라 색상 변경
        UpdateColor();
    }

    /// <summary>
    /// 시야 범위 파라미터 업데이트
    /// </summary>
    public void SetVisionParameters(float range, float angle)
    {
        visionRange = range;
        visionAngle = angle;
        GenerateVisionMesh();
    }

    /// <summary>
    /// 플레이어 감지 상태 설정
    /// </summary>
    public void SetDetectionState(bool detected)
    {
        isPlayerDetected = detected;
    }

    /// <summary>
    /// 시야 원뿔 메시 생성
    /// </summary>
    private void GenerateVisionMesh()
    {
        visionMesh.Clear();

        Vector3[] vertices = new Vector3[resolution + 2];
        int[] triangles = new int[resolution * 3];

        // 중심점 (원뿔의 꼭지점)
        vertices[0] = Vector3.zero;

        float currentAngle = -visionAngle / 2f;
        float angleStep = visionAngle / resolution;

        // 원뿔의 가장자리 정점 생성
        for (int i = 0; i <= resolution; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));

            // Raycast로 장애물 체크
            float currentRange = visionRange;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.TransformDirection(direction),
                out hit, visionRange, hunterAI != null ? hunterAI.GetVisionBlockerLayer() : 0))
            {
                // 장애물이 있으면 그 지점까지만
                currentRange = hit.distance;
            }

            vertices[i + 1] = direction * currentRange + Vector3.up * height;
            currentAngle += angleStep;
        }

        // 삼각형 생성
        for (int i = 0; i < resolution; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateNormals();
    }

    /// <summary>
    /// 감지 상태에 따라 색상 업데이트
    /// </summary>
    private void UpdateColor()
    {
        if (visionMaterial != null)
        {
            Color targetColor = isPlayerDetected ? detectedColor : normalColor;
            visionMaterial.color = Color.Lerp(visionMaterial.color, targetColor, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 기본 머티리얼 생성
    /// </summary>
    private Material CreateVisionMaterial()
    {
        // URP와 Built-in 모두 지원하는 Unlit/Transparent 쉐이더 사용
        Shader shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
        {
            // Unlit/Transparent가 없으면 Legacy Shaders 사용
            shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        }

        if (shader == null)
        {
            // 그래도 없으면 Sprites/Default 사용
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("[VisionCone] 적합한 쉐이더를 찾을 수 없습니다!");
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.color = normalColor;
        mat.renderQueue = 3000; // Transparent 렌더링

        return mat;
    }

    private void OnDestroy()
    {
        if (visionMesh != null)
        {
            Destroy(visionMesh);
        }

        if (visionMaterial != null && Application.isPlaying)
        {
            Destroy(visionMaterial);
        }
    }
}