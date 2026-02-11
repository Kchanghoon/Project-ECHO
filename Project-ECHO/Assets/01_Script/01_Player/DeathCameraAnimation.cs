using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DeathCameraAnimation : MonoBehaviour
{
    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Screen Fade")]
    private Image fadeImage; // Inspector에서 할당 안 함!
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private Color fadeColor = Color.red;

    [Header("Camera Tilt")]
    [SerializeField] private float fallRotationSpeed = 90f;
    [SerializeField] private Vector3 finalRotation = new Vector3(90f, 0f, 15f);

    [Header("Audio")]
    [SerializeField] private AudioClip deathScream;
    [SerializeField] private AudioClip hunterAttackSound;

    private Transform cameraTransform;
    private AudioSource audioSource;
    private Vector3 originalCameraRotation;
    private bool isPlaying = false;

    void Start()
    {
        Camera mainCam = GetComponentInChildren<Camera>();
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
            originalCameraRotation = cameraTransform.localEulerAngles;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ✅ Scene에서 DeathFadeImage 찾기
        FindFadeImage();
    }

    private void FindFadeImage()
    {
        Debug.Log("[DeathCameraAnimation] DeathFadeImage 찾기 시작...");

        // ✅ Tag로 찾기 (O(1) 수준으로 빠름)
        GameObject fadeImageObj = GameObject.FindGameObjectWithTag("DeathFadeImage");

        if (fadeImageObj != null)
        {
            fadeImage = fadeImageObj.GetComponent<Image>();
            Debug.Log("[DeathCameraAnimation] DeathFadeImage를 Tag로 찾았습니다!");
        }
        else
        {
            Debug.LogError("[DeathCameraAnimation] Tag 'DeathFadeImage'를 가진 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 초기 설정
        Color c = fadeColor;
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false);

        Debug.Log("[DeathCameraAnimation] DeathFadeImage 초기화 완료");
    }

    public void PlayDeathAnimation()
    {
        if (isPlaying) return;

        if (fadeImage == null)
        {
            Debug.LogError("[DeathCameraAnimation] fadeImage가 null입니다! 사망 연출 실행 불가.");
            return;
        }

        isPlaying = true;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (hunterAttackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hunterAttackSound);
        }

        StartCoroutine(CameraShake());
        StartCoroutine(CameraFall());

        yield return new WaitForSeconds(0.3f);
        if (deathScream != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathScream);
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FadeToBlack());

        Debug.Log("[DeathCameraAnimation] 사망 연출 완료");
    }

    private IEnumerator CameraShake()
    {
        if (cameraTransform == null) yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = shakeCurve.Evaluate(elapsed / shakeDuration) * shakeIntensity;

            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * strength;

            cameraTransform.localPosition = randomOffset;

            yield return null;
        }

        cameraTransform.localPosition = Vector3.zero;
    }

    private IEnumerator CameraFall()
    {
        if (cameraTransform == null) yield break;

        float elapsed = 0f;
        Vector3 startRotation = cameraTransform.localEulerAngles;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            cameraTransform.localEulerAngles = Vector3.Lerp(startRotation, finalRotation, t);

            yield return null;
        }

        cameraTransform.localEulerAngles = finalRotation;
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogError("[DeathCameraAnimation] fadeImage null - FadeToBlack 실행 불가");
            yield break;
        }

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color startColor = fadeColor;
        startColor.a = 0f;
        Color endColor = fadeColor;
        endColor.a = 1f;

        Debug.Log("[DeathCameraAnimation] Fade 시작");

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        fadeImage.color = endColor;
        Debug.Log("[DeathCameraAnimation] Fade 완료");
    }
}