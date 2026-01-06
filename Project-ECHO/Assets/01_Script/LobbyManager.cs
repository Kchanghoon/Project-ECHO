using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        // 버튼 클릭 이벤트 연결
        hostButton.onClick.AddListener(StartAsHost);
        clientButton.onClick.AddListener(StartAsClient);
    }

    private void StartAsHost()
    {
        // 1. 호스트로 네트워크 시작
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("호스트 시작 성공");
            // 2. 호스트가 Game 씬으로 전환 (연결된 클라이언트들도 함께 이동함)
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }

    private void StartAsClient()
    {
        // 클라이언트로 네트워크 시작 (호스트가 이미 열려있어야 함)
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("클라이언트 접속 시도 중...");
        }
    }
}