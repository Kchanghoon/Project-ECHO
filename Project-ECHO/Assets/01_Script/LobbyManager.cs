using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button startButton;

    private void Start()
    {    Debug.Log($"[Photon] AppIdRealtime={PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime}");

        // Photon 서버 연결
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player_" + Random.Range(0, 1000); // 닉네임 랜덤 설정
        PhotonNetwork.ConnectUsingSettings();

    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 서버 연결 완료");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 입장 완료");
        hostButton.interactable = true;
        clientButton.interactable = true;
    }

    private void Awake()
    {
hostButton.onClick.AddListener(CreateRoom);
    // [수정] OnJoinedRoom이 아니라 JoinRoom(또는 PhotonNetwork.JoinRandomRoom)을 호출해야 합니다.
    clientButton.onClick.AddListener(JoinRandomRoom); 
    startButton.onClick.AddListener(OnStartRoom);

    hostButton.interactable = false;
    clientButton.interactable = false;
    startButton.interactable = false;
    }



    private void CreateRoom()
    {
        hostButton.interactable = false; // 중복 클릭 방지
        clientButton.interactable = false;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4; // 최대 플레이어 수
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;

        PhotonNetwork.CreateRoom("Room_" + Random.Range(0, 10000), roomOptions);
    }

    private void JoinRandomRoom()
    {
        Debug.Log("랜덤 방 입장 시도...");
        clientButton.interactable = false; // 중복 클릭 방지
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        // 1. CurrentRoom 자체가 null인지 먼저 체크합니다.
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("방 입장 콜백은 호출되었으나, 아직 방 정보가 동기화되지 않았습니다. 잠시 대기합니다.");
            return;
        }

        Debug.Log($"방 입장 성공: {PhotonNetwork.CurrentRoom.Name}");

        // 2. 버튼 할당 여부도 함께 체크 (UI 에러 방지)
        if (startButton != null)
        {
            // 방장에게만 시작 버튼 활성화
            startButton.interactable = PhotonNetwork.IsMasterClient;

            // 클라이언트는 대기 문구로 변경 (선택 사항)
            if (!PhotonNetwork.IsMasterClient)
            {
                var btnText = startButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (btnText != null) btnText.text = "Waiting for Host...";
            }
        }
    }

    // 혹시 방장이 나가서 내가 방장이 된 경우에도 버튼을 켜줘야 합니다.
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = true;
        }
    }

    public void OnStartRoom()
    {
        // 방장만 호출 가능하도록 한 번 더 체크 (보안상 좋음)
        if (PhotonNetwork.IsMasterClient)
        {
            // 중복 클릭 방지
            startButton.interactable = false;
            // 다음 씬으로 모든 플레이어를 데리고 이동
            PhotonNetwork.LoadLevel("03_Combat");
        }
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("랜덤 방 입장 실패 - 새로운 방 생성");
        CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
    }

 

}