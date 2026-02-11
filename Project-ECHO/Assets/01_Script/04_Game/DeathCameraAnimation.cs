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
    [SerializeField] private Image fadeImage; // UI Canvas의 검은 이미지
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Camera Tilt (쓰러짐 효과)")]
    [SerializeField] private float fallRotationSpeed = 90f; // 초당 회전 각도
    [SerializeField] private Vector3 finalRotation = new Vector3(90f, 0f, 15f); // 최종 카메라 각도

    [Header("Audio")]
    [SerializeField] private AudioClip deathScream; // 플레이어 비명
    [SerializeField] private AudioClip hunterAttackSound; // 헌터 공격 소리

    private Transform cameraTransform;
    private AudioSource audioSource;
    private Vector3 originalCameraRotation;
    private bool isPlaying = false;

    void Start()
    {
        // 카메라 찾기
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

        // Fade 이미지 초기 설정
        if (fadeImage != null)
        {
            Color c = fadeColor;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void PlayDeathAnimation()
    {
        if (isPlaying) return;
        isPlaying = true;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. 헌터 공격 소리
        if (hunterAttackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hunterAttackSound);
        }

        // 2. 카메라 흔들림 + 회전 (쓰러지는 느낌)
        StartCoroutine(CameraShake());
        StartCoroutine(CameraFall());

        // 3. 플레이어 비명 (약간 딜레이)
        yield return new WaitForSeconds(0.3f);
        if (deathScream != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathScream);
        }

        // 4. 화면 서서히 어둡게
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

            // 랜덤 흔들림
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * strength;

            cameraTransform.localPosition = randomOffset;

            yield return null;
        }

        // 원래 위치로
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

            // 부드럽게 회전 (쓰러지는 효과)
            cameraTransform.localEulerAngles = Vector3.Lerp(startRotation, finalRotation, t);

            yield return null;
        }

        cameraTransform.localEulerAngles = finalRotation;
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        Color startColor = fadeColor;
        startColor.a = 0f;
        Color endColor = fadeColor;
        endColor.a = 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            fadeImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        fadeImage.color = endColor;
    }
}